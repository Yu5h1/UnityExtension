// Bodies made of a few particles (a fish, a fragment) and the head / middle / tail frame derived
// from them (ParticleExtensions EXT-10). Behaviour kernels and the articulated mesh shader both
// include this file, so physics and drawing follow the same frame. Include ParticleWorldData.hlsl
// first. The C# mirror of ParticleBody is ParticleBodyData (64 bytes); topology values match
// ParticleBodyTopology. scale.w is the drag coefficient, dimensions.w the colour as RGBA8 bits, and
// flags bit 0 is set while the body's entity is simulated.

#ifndef YU5H1LIB_PARTICLE_BODY_FRAME
#define YU5H1LIB_PARTICLE_BODY_FRAME

struct ParticleBody
{
    uint particleStart;
    uint particleCount;
    uint topology;
    uint flags;
    float4 scale;
    float4 spawnRotation;
    float4 dimensions;
};

struct BodyFrame
{
    float3 center;
    float3 tangent;
    float3 side;
    float3 normal;
};

static const uint BODY_SINGLE = 0u;
static const uint BODY_CHAIN_3 = 3u;
static const uint BODY_GUIDE_4 = 4u;
static const uint BODY_DUAL_RAIL_6 = 6u;
static const uint BODY_ARTICULATED_12 = 12u;
static const uint BODY_FLAG_SIMULATED = 1u;
static const float BODY_TWO_PI = 6.28318530718;

float4 BodyColor(ParticleBody body)
{
    uint packed = asuint(body.dimensions.w);
    return float4(packed & 0xffu, (packed >> 8) & 0xffu, (packed >> 16) & 0xffu, packed >> 24) / 255.0;
}

float Hash01(uint value)
{
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return (value & 0x00ffffffu) / 16777215.0;
}

float3 SafePerpendicular(float3 candidate, float3 tangent, float3 fallback)
{
    float3 projected = candidate - tangent * dot(candidate, tangent);
    float3 fallbackProjected = fallback - tangent * dot(fallback, tangent);
    float candidateLength = length(candidate);
    float projectedLength = length(projected);
    float fallbackLength = length(fallbackProjected);
    float confidence = candidateLength > 1e-9 ? saturate(projectedLength / (0.1 * candidateLength)) : 0.0;
    float3 projectedDirection = projectedLength > 1e-9 ? projected / projectedLength : float3(0.0, 0.0, 0.0);
    float3 fallbackDirection = fallbackLength > 1e-9 ? fallbackProjected / fallbackLength : float3(0.0, 0.0, 0.0);
    float3 blended = lerp(fallbackDirection, projectedDirection, confidence);
    float blendedLength = length(blended);
    if (blendedLength > 1e-6)
        return blended / blendedLength;
    float3 magnitude = abs(tangent);
    float3 axis = magnitude.x <= magnitude.y && magnitude.x <= magnitude.z
        ? float3(1.0, 0.0, 0.0)
        : (magnitude.y <= magnitude.z ? float3(0.0, 1.0, 0.0) : float3(0.0, 0.0, 1.0));
    return normalize(axis - tangent * dot(axis, tangent));
}

float3 BisectDirections(float3 first, float3 second, float3 fallback)
{
    float3 sum = first + second;
    float sumLength = length(sum);
    float confidence = saturate(sumLength / 0.35);
    float3 sumDirection = sumLength > 1e-9 ? sum / sumLength : float3(0.0, 0.0, 0.0);
    float3 blended = lerp(fallback, sumDirection, confidence);
    float blendedLength = length(blended);
    return blendedLength > 1e-6 ? blended / blendedLength : fallback;
}

float3 LimitMagnitude(float3 value, float maximumLength)
{
    float valueLength = length(value);
    if (valueLength <= maximumLength || valueLength <= 1e-6)
        return value;
    return value * (maximumLength / valueLength);
}

float3 BodyControlCenter(ParticleBody body, uint control)
{
    uint start = body.particleStart;
    if (body.topology == BODY_SINGLE)
        return _Position[start].xyz;
    if (body.topology == BODY_DUAL_RAIL_6)
    {
        uint pair = start + control * 2u;
        return (_Position[pair].xyz + _Position[pair + 1u].xyz) * 0.5;
    }
    if (body.topology == BODY_ARTICULATED_12)
    {
        uint group = start + control * 4u;
        return (_Position[group].xyz + _Position[group + 1u].xyz + _Position[group + 2u].xyz + _Position[group + 3u].xyz) * 0.25;
    }
    return _Position[start + control].xyz;
}

float3 BodyControlVelocity(ParticleBody body, uint control)
{
    uint start = body.particleStart;
    if (body.topology == BODY_SINGLE)
        return _Velocity[start].xyz;
    if (body.topology == BODY_DUAL_RAIL_6)
    {
        uint pair = start + control * 2u;
        return (_Velocity[pair].xyz + _Velocity[pair + 1u].xyz) * 0.5;
    }
    if (body.topology == BODY_ARTICULATED_12)
    {
        uint group = start + control * 4u;
        return (_Velocity[group].xyz + _Velocity[group + 1u].xyz + _Velocity[group + 2u].xyz + _Velocity[group + 3u].xyz) * 0.25;
    }
    return _Velocity[start + control].xyz;
}

float3 BodySideCandidate(ParticleBody body, uint control, float3 middle)
{
    uint start = body.particleStart;
    if (body.topology == BODY_GUIDE_4)
        return _Position[start + 3u].xyz - middle;
    if (body.topology == BODY_DUAL_RAIL_6)
    {
        uint pair = start + control * 2u;
        return _Position[pair].xyz - _Position[pair + 1u].xyz;
    }
    if (body.topology == BODY_ARTICULATED_12)
    {
        uint group = start + control * 4u;
        return (_Position[group].xyz + _Position[group + 3u].xyz) - (_Position[group + 1u].xyz + _Position[group + 2u].xyz);
    }
    return Rotate(body.spawnRotation, float3(1.0, 0.0, 0.0));
}

// Frame of control 0 (head), 1 (middle) or 2 (tail): tangent along the body towards the head,
// side from the body's own side particles, normal completing a right-handed frame.
BodyFrame GetBodyFrame(ParticleBody body, uint control)
{
    float3 head = BodyControlCenter(body, 0u);
    float3 middle = BodyControlCenter(body, 1u);
    float3 tail = BodyControlCenter(body, 2u);
    BodyFrame frame;
    frame.center = control == 0u ? head : control == 1u ? middle : tail;
    float3 headSegment = head - middle;
    float3 tailSegment = middle - tail;
    float headSegmentLength = length(headSegment);
    float tailSegmentLength = length(tailSegment);
    float3 headDirection = headSegmentLength > 1e-6 ? headSegment / headSegmentLength : float3(0.0, 0.0, 0.0);
    float3 tailDirection = tailSegmentLength > 1e-6 ? tailSegment / tailSegmentLength : float3(0.0, 0.0, 0.0);
    float3 baseY = Rotate(body.spawnRotation, float3(0.0, 1.0, 0.0));
    float3 chainFallback = headSegmentLength > 1e-6 ? headDirection : (tailSegmentLength > 1e-6 ? tailDirection : baseY);
    float3 middleTangent = BisectDirections(headDirection, tailDirection, chainFallback);
    float3 tangentCandidate = control == 0u ? headSegment : control == 1u ? middleTangent : tailSegment;
    float tangentLength = length(tangentCandidate);
    frame.tangent = body.topology == BODY_SINGLE
        ? baseY
        : (tangentLength > 1e-5 ? tangentCandidate / tangentLength : chainFallback);
    float3 baseX = Rotate(body.spawnRotation, float3(1.0, 0.0, 0.0));
    float3 baseZ = Rotate(body.spawnRotation, float3(0.0, 0.0, 1.0));
    float3 sideSource = BodySideCandidate(body, 1u, middle);
    frame.side = SafePerpendicular(sideSource, frame.tangent, baseX);
    float3 middleSide = SafePerpendicular(sideSource, middleTangent, baseX);
    if (dot(frame.side, middleSide) < 0.0)
        frame.side = -frame.side;
    frame.normal = SafePerpendicular(cross(frame.side, frame.tangent), frame.tangent, baseZ);
    frame.side = normalize(cross(frame.tangent, frame.normal));
    return frame;
}

// Particle ranges, relative to particleStart, that make up the head, middle and tail controls.
void BodyRanges(ParticleBody body, out uint headStart, out uint headCount, out uint middleStart, out uint middleCount, out uint tailStart, out uint tailCount)
{
    headStart = 0u;
    headCount = 1u;
    middleStart = 1u;
    middleCount = 1u;
    tailStart = 2u;
    tailCount = 1u;
    if (body.topology == BODY_DUAL_RAIL_6)
    {
        headCount = 2u;
        middleStart = 2u;
        middleCount = 2u;
        tailStart = 4u;
        tailCount = 2u;
    }
    else if (body.topology == BODY_ARTICULATED_12)
    {
        headCount = 4u;
        middleStart = 4u;
        middleCount = 4u;
        tailStart = 8u;
        tailCount = 4u;
    }
}

#endif
