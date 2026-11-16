using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Base polyhedra of the rigid cluster layouts, and the triangle lists that close them
    ///   (ParticleExtensions EXT-10; Unified-Solver's SolverHullShapes). A fragment is one of these
    ///   with its vertices moved by a bounded amount, never a random cloud, so a degenerate or
    ///   self-intersecting fragment cannot be produced. Vertices are in units of the half extent on
    ///   each axis: a corner reads 1 and an axis point reads 1. Faces are wound counter-clockwise
    ///   seen from outside. Physics and drawing read the same tables.
    /// </summary>
    public static class ParticleHullShapes
    {
        /// <summary>
        ///   Largest fraction of the half extent a vertex may be displaced by.
        /// </summary>
        public const float MaximumJitter = 0.35f;

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

        /// <summary>
        ///   Particles of a rigid cluster topology, or 0 for any other.
        /// </summary>
        public static int ClusterParticles(ParticleBodyTopology topology)
        {
            switch (topology)
            {
                case ParticleBodyTopology.RigidCluster4: return 4;
                case ParticleBodyTopology.RigidCluster6: return 6;
                case ParticleBodyTopology.RigidCluster8: return 8;
                default: return 0;
            }
        }

        public static ParticleBodyTopology ClusterFor(int particles)
        {
            switch (particles)
            {
                case 4: return ParticleBodyTopology.RigidCluster4;
                case 6: return ParticleBodyTopology.RigidCluster6;
                default: return ParticleBodyTopology.RigidCluster8;
            }
        }

        /// <summary>
        ///   The unjittered corners of a rigid cluster, or null for any other topology.
        /// </summary>
        public static Vector3[] BaseVertices(ParticleBodyTopology topology)
        {
            switch (topology)
            {
                case ParticleBodyTopology.RigidCluster4: return Tetrahedron;
                case ParticleBodyTopology.RigidCluster6: return Octahedron;
                case ParticleBodyTopology.RigidCluster8: return Box;
                default: return null;
            }
        }

        /// <summary>
        ///   Triangle indices closing the cluster's vertices, or null for any other topology.
        /// </summary>
        public static int[] Faces(ParticleBodyTopology topology)
        {
            switch (topology)
            {
                case ParticleBodyTopology.RigidCluster4: return TetrahedronFaces;
                case ParticleBodyTopology.RigidCluster6: return OctahedronFaces;
                case ParticleBodyTopology.RigidCluster8: return BoxFaces;
                default: return null;
            }
        }
    }
}
