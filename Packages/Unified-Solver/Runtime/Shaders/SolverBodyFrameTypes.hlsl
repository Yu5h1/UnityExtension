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

// Direction perpendicular to tangent, taken from candidate where that is
// meaningful and from fallback where it is not.
//
// Judged relative to the candidate's own length, not against a fixed floor. An
// absolute threshold lets through a projection that is numerically tiny but
// still above it, and normalizing that returns rounding error as a direction,
// which flips from step to step. The body frame is built from this, so a flip
// swaps the body's left and right, and anything reading the frame swings with
// it.
//
// The handover is a blend rather than a switch, so the direction cannot jump as
// the candidate degenerates.
float3 SafePerpendicular(
    float3 candidate,
    float3 tangent,
    float3 fallback)
{
    float3 projected =
        candidate -
        tangent * dot(candidate, tangent);
    float3 fallbackProjected =
        fallback -
        tangent * dot(fallback, tangent);

    float candidateLength = length(candidate);
    float projectedLength = length(projected);
    float fallbackLength = length(fallbackProjected);

    // How much of the candidate survived the projection. Below a tenth of its
    // own length it is treated as carrying no direction at all.
    float confidence =
        candidateLength > 1e-9
            ? saturate(
                projectedLength /
                (0.1 * candidateLength))
            : 0.0;

    float3 projectedDirection =
        projectedLength > 1e-9
            ? projected / projectedLength
            : float3(0.0, 0.0, 0.0);
    float3 fallbackDirection =
        fallbackLength > 1e-9
            ? fallbackProjected / fallbackLength
            : float3(0.0, 0.0, 0.0);

    float3 blended = lerp(
        fallbackDirection,
        projectedDirection,
        confidence);
    float blendedLength = length(blended);
    if (blendedLength > 1e-6)
        return blended / blendedLength;

    // Both degenerate. Pick the world axis furthest from the tangent so the
    // projection that follows is as large, and as stable, as it can be.
    float3 magnitude = abs(tangent);
    float3 axis =
        magnitude.x <= magnitude.y &&
        magnitude.x <= magnitude.z
            ? float3(1.0, 0.0, 0.0)
            : (magnitude.y <= magnitude.z
                ? float3(0.0, 1.0, 0.0)
                : float3(0.0, 0.0, 1.0));
    return normalize(
        axis -
        tangent * dot(axis, tangent));
}

// Unit axis bisecting two segment directions, handed over to a fallback as the
// two approach opposition.
//
// Their sum has length 2 * cos(fold / 2), so it shrinks to nothing at a hairpin,
// and long before that its direction is set by whatever asymmetry is left
// between the two segments: the direction error is roughly that asymmetry
// divided by the sum's length, so it grows without bound and reverses outright
// the moment the fold passes through straight. Everything downstream reads the
// middle's frame and the drive rebuilds its pose from it, so a reversal swaps
// the body's two ends over and the next step measures the reversal again. That
// is a spin that feeds itself, not a one-off glitch.
//
// Testing the sum against a fixed floor cannot catch this. A sum of 3e-4 is
// thirty times any such floor while carrying no usable direction at all; the
// test has to be against the sum's own natural scale of 2, which is the same
// relative-length reasoning SafePerpendicular above already needed.
//
// The fallback must be asymmetric between the two ends or it degenerates in the
// same place for the same reason. One segment's own direction is asymmetric,
// stays unit length, and moves continuously, so the handover neither jumps nor
// flips. It also leans the axis toward the half of the body that still has a
// direction, which is what lets a drive built on this frame pull a folded body
// back open instead of holding it shut.
float3 BisectDirections(
    float3 first,
    float3 second,
    float3 fallback)
{
    float3 sum = first + second;
    float sumLength = length(sum);

    // Full confidence down to a fold of about 160 degrees, none at 180.
    float confidence = saturate(sumLength / 0.35);
    float3 sumDirection =
        sumLength > 1e-9
            ? sum / sumLength
            : float3(0.0, 0.0, 0.0);

    float3 blended = lerp(
        fallback,
        sumDirection,
        confidence);
    float blendedLength = length(blended);
    return
        blendedLength > 1e-6
            ? blended / blendedLength
            : fallback;
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
