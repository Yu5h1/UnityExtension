using UnityEngine;

namespace Yu5h1Lib.ParticlePhysics
{
    // One particle an anchor wants held at a place.
    //
    // Identified by a flat index local to its own body, never by a coordinate.
    // The body maps that to whatever its backend uses, so the core never learns
    // how any backend numbers its particles.
    public struct PhysicsParticleTarget
    {
        public int index;
        public Vector3 position;
    }

    // Where a body's particles are, and how to hold some of them still.
    //
    // This is the seam that lets one anchor component serve bodies that have
    // nothing else in common. `PhysicsParticleAnchor` knows only "these
    // particles, at these world positions"; everything backend-specific --
    // which buffer, which compute kernel, which private field the particle
    // offset hides behind -- lives on the other side of it.
    //
    // The two vendored generators cannot implement it themselves. They are
    // read-only dependencies, so each needs a wrapper component in the
    // extension. That is the only reason the glue exists; a body written here
    // would implement this directly and need no wrapper.
    public interface IPhysicsParticleSource
    {
        // How many particles are selectable, numbered 0 .. Count-1.
        //
        // A flat index is the identity, always. An earlier draft used a
        // Vector2Int grid coordinate, which forced a rope to pretend to be
        // (segments, 1) and could not address a 3D lattice at all -- and a
        // lattice body is exactly where this is heading. Shape belongs to
        // display, never to identity.
        int ParticleCount { get; }

        // Display only. The lattice dimensions when the particles form one,
        // zero on any axis when they do not.
        //
        // The editor uses it to draw grid lines and to label a particle as
        // (x, y) instead of a bare number. Nothing resolves identity through
        // it, so a source that is an unstructured point cloud simply returns
        // zero and still works -- it just draws as points.
        Vector3Int LayoutSize { get; }

        // Where a particle sits before anything simulates it.
        //
        // Must work with no solver and no particles, because the editor calls it
        // to capture offsets and draw handles, and in edit mode the generators
        // have not spawned anything yet. So it reads the body's own layout
        // maths, never a particle buffer.
        bool TryGetRestPosition(
            int index,
            out Vector3 worldPosition);

        // Pin these particles at these positions for this step.
        //
        // False when the source cannot resolve its particles yet -- the body has
        // not spawned, or the compatibility contract is unavailable. The caller
        // treats that as "not ready", not as an error, because it is the normal
        // state for the first few frames.
        bool TryApplyTargets(
            PhysicsParticleTarget[] targets,
            int count);
    }
}
