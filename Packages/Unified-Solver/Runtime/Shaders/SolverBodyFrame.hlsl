#ifndef YU5H1_SOLVER_BODY_FRAME_INCLUDED
#define YU5H1_SOLVER_BODY_FRAME_INCLUDED

// Body frame construction shared by the modifier compute shader and the
// articulated mesh shader.
//
// Requires SolverBodyFrameTypes.hlsl and the following buffers to be declared
// before this file is included:
//
//   _Particles   RWStructuredBuffer<Particle> or StructuredBuffer<Particle>
//   _Instances   StructuredBuffer<SolverInstance>
//
// Every existing topology carries exactly three longitudinal controls
// (0 = head, 1 = middle, 2 = tail). Off-spine particles supply the body frame,
// not extra bending freedom.

float3 ControlCenter(
    SolverInstance instance,
    uint control)
{
    uint particleBase =
        (uint)instance.particleOffset;
    if (instance.topology ==
        TOPOLOGY_SINGLE)
    {
        return
            _Particles[particleBase].position;
    }

    if (instance.topology ==
        TOPOLOGY_DUAL_RAIL_6)
    {
        uint pair =
            particleBase + control * 2u;
        return (
            _Particles[pair].position +
            _Particles[pair + 1u].position) *
            0.5;
    }

    if (instance.topology ==
        TOPOLOGY_ARTICULATED_12)
    {
        uint start =
            particleBase + control * 4u;
        return (
            _Particles[start].position +
            _Particles[start + 1u].position +
            _Particles[start + 2u].position +
            _Particles[start + 3u].position) *
            0.25;
    }

    return _Particles[
        particleBase + control].position;
}

float3 ControlVelocity(
    SolverInstance instance,
    uint control)
{
    uint particleBase =
        (uint)instance.particleOffset;
    if (instance.topology ==
        TOPOLOGY_SINGLE)
    {
        return
            _Particles[particleBase].velocity;
    }

    if (instance.topology ==
        TOPOLOGY_DUAL_RAIL_6)
    {
        uint pair =
            particleBase + control * 2u;
        return (
            _Particles[pair].velocity +
            _Particles[pair + 1u].velocity) *
            0.5;
    }

    if (instance.topology ==
        TOPOLOGY_ARTICULATED_12)
    {
        uint start =
            particleBase + control * 4u;
        return (
            _Particles[start].velocity +
            _Particles[start + 1u].velocity +
            _Particles[start + 2u].velocity +
            _Particles[start + 3u].velocity) *
            0.25;
    }

    return _Particles[
        particleBase + control].velocity;
}

// Direction across the body at a given control, before orthogonalisation.
// Topologies with off-spine particles report a real body-frame vector that
// follows the body when it rolls. Chain3 has no off-spine particle and can
// only fall back to the spawn frame.
float3 SideCandidate(
    SolverInstance instance,
    uint control,
    float3 middle)
{
    uint particleBase =
        (uint)instance.particleOffset;
    if (instance.topology ==
        TOPOLOGY_GUIDE_4)
    {
        return
            _Particles[
                particleBase + 3u].position -
            middle;
    }

    if (instance.topology ==
        TOPOLOGY_DUAL_RAIL_6)
    {
        uint pair =
            particleBase + control * 2u;
        return
            _Particles[pair].position -
            _Particles[pair + 1u].position;
    }

    if (instance.topology ==
        TOPOLOGY_ARTICULATED_12)
    {
        uint start =
            particleBase + control * 4u;
        return
            (_Particles[start].position +
             _Particles[start + 3u].position) -
            (_Particles[start + 1u].position +
             _Particles[start + 2u].position);
    }

    return RotateByQuaternion(
        float3(1.0, 0.0, 0.0),
        instance.spawnRotation);
}

ControlFrame GetFrame(
    SolverInstance instance,
    uint control)
{
    float3 head =
        ControlCenter(instance, 0u);
    float3 middle =
        ControlCenter(instance, 1u);
    float3 tail =
        ControlCenter(instance, 2u);

    ControlFrame frame;
    frame.center =
        control == 0u
            ? head
            : control == 1u
                ? middle
                : tail;
    float3 tangentCandidate =
        control == 0u
            ? head - middle
            : control == 1u
                ? head - tail
                : middle - tail;
    float3 baseY =
        RotateByQuaternion(
            float3(0.0, 1.0, 0.0),
            instance.spawnRotation);
    // A fully folded body can collapse head onto tail while the arc length is
    // still non-zero. normalize() would produce NaN and poison every particle
    // in the instance, so fall back to the spawn axis.
    float tangentLength =
        length(tangentCandidate);
    frame.tangent =
        (instance.topology == TOPOLOGY_SINGLE ||
         tangentLength <= 1e-5)
            ? baseY
            : tangentCandidate / tangentLength;

    float3 baseX =
        RotateByQuaternion(
            float3(1.0, 0.0, 0.0),
            instance.spawnRotation);
    float3 baseZ =
        RotateByQuaternion(
            float3(0.0, 0.0, 1.0),
            instance.spawnRotation);
    frame.side = SafePerpendicular(
        SideCandidate(
            instance,
            control,
            middle),
        frame.tangent,
        baseX);
    frame.normal =
        SafePerpendicular(
            cross(
                frame.side,
                frame.tangent),
            frame.tangent,
            baseZ);
    frame.side = normalize(
        cross(
            frame.tangent,
            frame.normal));
    return frame;
}

void BodyRanges(
    SolverInstance instance,
    out uint headStart,
    out uint headCount,
    out uint middleStart,
    out uint middleCount,
    out uint tailStart,
    out uint tailCount)
{
    headStart = 0u;
    headCount = 1u;
    middleStart = 1u;
    middleCount = 1u;
    tailStart = 2u;
    tailCount = 1u;

    if (instance.topology ==
        TOPOLOGY_DUAL_RAIL_6)
    {
        headCount = 2u;
        middleStart = 2u;
        middleCount = 2u;
        tailStart = 4u;
        tailCount = 2u;
    }
    else if (instance.topology ==
        TOPOLOGY_ARTICULATED_12)
    {
        headCount = 4u;
        middleStart = 4u;
        middleCount = 4u;
        tailStart = 8u;
        tailCount = 4u;
    }
}

#endif
