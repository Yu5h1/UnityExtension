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
    // Follow the chain link by link. The middle takes the mean of the two
    // segment directions rather than the span from head to tail.
    //
    // The span and the mean both collapse when the two halves meet, so neither
    // is safe on its own; the mean is preferred only because it stays useful
    // further into the fold. What actually makes the middle survive a hairpin is
    // BisectDirections handing over to a direction that cannot invert. Reading
    // the raw span here was worse in a second way as well: it made the middle
    // the only control whose tangent came from somewhere other than the chain.
    float3 headSegment = head - middle;
    float3 tailSegment = middle - tail;
    float headSegmentLength = length(headSegment);
    float tailSegmentLength = length(tailSegment);
    float3 headSegmentDirection =
        headSegmentLength > 1e-6
            ? headSegment / headSegmentLength
            : float3(0.0, 0.0, 0.0);
    float3 tailSegmentDirection =
        tailSegmentLength > 1e-6
            ? tailSegment / tailSegmentLength
            : float3(0.0, 0.0, 0.0);

    float3 baseY =
        RotateByQuaternion(
            float3(0.0, 1.0, 0.0),
            instance.spawnRotation);
    // Fall back along the chain before falling back to the spawn axis.
    //
    // spawnRotation is written once when the body appears and never again, so
    // for anything that has since tumbled it points somewhere unrelated to the
    // body's current pose. Reaching for it mid-flight snaps the whole frame to
    // that stale orientation in a single step, which reads as the body flipping
    // over from one frame to the next. Either half of the chain still carries a
    // usable direction long after their mean has cancelled out, and it moves
    // continuously, so try those first.
    float3 chainFallback =
        headSegmentLength > 1e-6
            ? headSegmentDirection
            : (tailSegmentLength > 1e-6
                ? tailSegmentDirection
                : baseY);
    // The one axis the whole body is built on, conditioned so it cannot invert.
    // See BisectDirections: the mean of the two halves is what collapses when
    // head and tail meet, and normalising it there hands back a direction made
    // of noise that reverses from step to step.
    float3 middleTangent = BisectDirections(
        headSegmentDirection,
        tailSegmentDirection,
        chainFallback);

    float3 tangentCandidate =
        control == 0u
            ? headSegment
            : control == 1u
                ? middleTangent
                : tailSegment;
    float tangentLength =
        length(tangentCandidate);
    frame.tangent =
        instance.topology == TOPOLOGY_SINGLE
            ? baseY
            : (tangentLength > 1e-5
                ? tangentCandidate / tangentLength
                : chainFallback);

    float3 baseX =
        RotateByQuaternion(
            float3(1.0, 0.0, 0.0),
            instance.spawnRotation);
    float3 baseZ =
        RotateByQuaternion(
            float3(0.0, 0.0, 1.0),
            instance.spawnRotation);
    // One cross direction for the whole body, taken from the middle and then
    // projected onto each segment's own tangent plane.
    //
    // Reading each segment's own pair instead gives the three frames three
    // independent sources for the body's right, with nothing holding them
    // together. Any twist in the physics, however small, then skins as a body
    // wrung along its length, because the mesh is free to rotate about the
    // spine between one control and the next.
    //
    // Whole-body roll still comes through, since the middle's pair rolls with
    // the body. Only disagreement between segments is suppressed.
    float3 sideSource = SideCandidate(
        instance,
        1u,
        middle);
    frame.side = SafePerpendicular(
        sideSource,
        frame.tangent,
        baseX);

    // Settle the sign against the middle's own frame.
    //
    // Sharing the source vector is not enough on its own: each control projects
    // it onto a different tangent plane, and once the body bends far enough the
    // projection for one of them lands the other way round. Blending two frames
    // whose cross directions oppose sweeps the width through zero at the
    // halfway point, so the mesh pinches to nothing there and flares out either
    // side of it. That is the hourglass, and it is a sign flip rather than a
    // twist.
    //
    // All three controls settle their sign against this one, so it is the single
    // place a whole-body mirror can come from: the instant this tangent jumps or
    // reverses, all three flip together. That is why it is worth conditioning
    // rather than merely guarding against NaN, and why the fallback inside
    // BisectDirections has to stay on the chain instead of reaching for the
    // spawn axis, which is stale for anything that has since tumbled.
    float3 middleSide = SafePerpendicular(
        sideSource,
        middleTangent,
        baseX);
    if (dot(frame.side, middleSide) < 0.0)
        frame.side = -frame.side;

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
