using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // Base polyhedra for the rigid cluster variants, and the triangle lists that
    // close them.
    //
    // A fragment is one of these with its vertices moved, not a random cloud of
    // points inside a bounding box. That is the whole reason the variants can be
    // trusted: with a fixed face list and a bounded displacement, a degenerate
    // or self-intersecting fragment is not something the generator has to detect
    // and reject, it is something it cannot produce. Rest particles that are
    // coplanar would leave the solver's shape matching with a rank-deficient
    // covariance and no defined rotation, and the face list is also what the
    // renderer draws, so a fragment that folded through itself would be visible
    // as well as unstable.
    //
    // Both the shape source and the hull mesh builder read these tables, so the
    // physics and the drawn surface cannot disagree about which points are
    // corners or which corners form a face.
    //
    // Vertices are in units of the half extent on each axis, not on the unit
    // sphere: a corner reads 1 and an axis point reads 1, so the profile's
    // dimensions bound the tetrahedron and the box exactly and span the
    // octahedron's axes exactly. Normalising them instead would shrink the
    // corner solids to 58% of their stated dimensions, and would silently
    // change every existing RigidCluster4 profile, whose four points are these
    // same alternating cube corners.
    //
    // Faces are wound counter-clockwise seen from outside.
    public static class SolverHullShapes
    {
        // Four alternating corners of a cube: the regular tetrahedron.
        static readonly Vector3[] Tetrahedron =
        {
            new Vector3(1f, 1f, 1f),
            new Vector3(1f, -1f, -1f),
            new Vector3(-1f, 1f, -1f),
            new Vector3(-1f, -1f, 1f)
        };

        static readonly int[] TetrahedronFaces =
        {
            0, 1, 2,
            0, 3, 1,
            0, 2, 3,
            1, 3, 2
        };

        static readonly Vector3[] Octahedron =
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, -1f, 0f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, -1f)
        };

        static readonly int[] OctahedronFaces =
        {
            0, 2, 4,
            2, 1, 4,
            1, 3, 4,
            3, 0, 4,
            2, 0, 5,
            1, 2, 5,
            3, 1, 5,
            0, 3, 5
        };

        // Cube corners indexed by bit: 1 = +x, 2 = +y, 4 = +z.
        static readonly Vector3[] Box =
        {
            new Vector3(-1f, -1f, -1f),
            new Vector3(1f, -1f, -1f),
            new Vector3(-1f, 1f, -1f),
            new Vector3(1f, 1f, -1f),
            new Vector3(-1f, -1f, 1f),
            new Vector3(1f, -1f, 1f),
            new Vector3(-1f, 1f, 1f),
            new Vector3(1f, 1f, 1f)
        };

        static readonly int[] BoxFaces =
        {
            1, 3, 7, 1, 7, 5,
            0, 4, 6, 0, 6, 2,
            2, 6, 7, 2, 7, 3,
            0, 1, 5, 0, 5, 4,
            4, 5, 7, 4, 7, 6,
            0, 2, 3, 0, 3, 1
        };

        public static Vector3[] BaseVertices(
            SolverParticleTopology topology)
        {
            switch (topology)
            {
                case SolverParticleTopology.RigidCluster4:
                    return Tetrahedron;
                case SolverParticleTopology.RigidCluster6:
                    return Octahedron;
                case SolverParticleTopology.RigidCluster8:
                    return Box;
                default:
                    return null;
            }
        }

        public static int[] Faces(
            SolverParticleTopology topology)
        {
            switch (topology)
            {
                case SolverParticleTopology.RigidCluster4:
                    return TetrahedronFaces;
                case SolverParticleTopology.RigidCluster6:
                    return OctahedronFaces;
                case SolverParticleTopology.RigidCluster8:
                    return BoxFaces;
                default:
                    return null;
            }
        }

        // Largest fraction of the half-extent a vertex may be displaced by.
        //
        // A tetrahedron stays a valid tetrahedron under any displacement short
        // of coplanar, but the octahedron and the box have corners that can be
        // pushed inside the hull of the others, at which point the fixed face
        // list no longer describes the shape it is drawing. This bound is well
        // inside where that becomes possible for either.
        public const float MaximumJitter = 0.35f;
    }
}
