#ifndef YU5H1_SOLVER_BODY_FRAME_TYPES_INCLUDED
#define YU5H1_SOLVER_BODY_FRAME_TYPES_INCLUDED

// Shared data contract and frame-independent math for the Unified Solver
// extension. Include this before declaring the particle buffers, then include
// SolverBodyFrame.hlsl after them.
//
// Both the modifier compute shader and the articulated mesh shader must build
// the body frame from the same code. When they diverge the physics bends in
// one plane while the mesh is skinned in another.

struct Particle
{
    float3 position;
    float3 velocity;
    float3 prevPosition;
    float invMass;
    int phase;
    float3 color;
    uint visible;
};

struct SolverInstance
{
    int particleOffset;
    int particleCount;
    int constraintOffset;
    int constraintCount;
    int rigidBodyOffset;
    int rigidBodyCount;
    int topology;
    int profileId;
    float3 scale;
    float padding;
    float4 spawnRotation;
};

struct ControlFrame
{
    float3 center;
    float3 tangent;
    float3 side;
    float3 normal;
};

static const int TOPOLOGY_SINGLE = 0;
static const int TOPOLOGY_CHAIN_3 = 3;
static const int TOPOLOGY_GUIDE_4 = 4;
static const int TOPOLOGY_DUAL_RAIL_6 = 6;
static const int TOPOLOGY_ARTICULATED_12 = 12;
static const float TWO_PI = 6.28318530718;

float Hash01(uint value)
{
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return
        (value & 0x00ffffffu) /
        16777215.0;
}

float3 RotateByQuaternion(
    float3 value,
    float4 rotation)
{
    float3 t =
        2.0 * cross(rotation.xyz, value);
    return value +
        rotation.w * t +
        cross(rotation.xyz, t);
}

float3 SafePerpendicular(
    float3 candidate,
    float3 tangent,
    float3 fallback)
{
    float3 projected =
        candidate -
        tangent * dot(candidate, tangent);
    float projectedLength = length(projected);
    if (projectedLength > 1e-5)
        return projected / projectedLength;

    projected =
        fallback -
        tangent * dot(fallback, tangent);
    projectedLength = length(projected);
    if (projectedLength > 1e-5)
        return projected / projectedLength;

    float3 axis =
        abs(tangent.x) < 0.8
            ? float3(1.0, 0.0, 0.0)
            : float3(0.0, 0.0, 1.0);
    return normalize(
        axis -
        tangent * dot(axis, tangent));
}

float3 LimitMagnitude(
    float3 value,
    float maximumLength)
{
    float valueLength = length(value);
    if (valueLength <= maximumLength ||
        valueLength <= 1e-6)
    {
        return value;
    }

    return
        value *
        (maximumLength / valueLength);
}

#endif
