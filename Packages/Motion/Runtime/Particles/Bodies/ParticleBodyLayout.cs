using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   The particle arrangement of a body: fish, fragments and other few-particle things
    ///   (ParticleExtensions EXT-10). Each topology matches one of Unified-Solver's, with the same
    ///   local positions from <c>SolverParticleEmitter.BuildLocalShape</c>: half sizes are the base
    ///   dimensions times the body scale times 0.5, and the spawn rotation is the entity pose. With a
    ///   <see cref="ParticleBodyShapeSource"/> each variant is one of its templates and the topology
    ///   is whatever the template realizes.
    /// </summary>
    public sealed class ParticleBodyLayout : PhysicsParticleLayout
    {
        [SerializeField]
        ParticleBodyTopology _topology = ParticleBodyTopology.Chain3;
        [Tooltip("X = width, Y = length or height, Z = thickness or depth, in meters, before the body scale.")]
        [SerializeField]
        Vector3 _baseDimensions = new Vector3(0.12f, 0.4f, 0.08f);
        [Tooltip("Overrides the topology per variant. Also decides which topology each variant realizes.")]
        [Inline]
        [SerializeField]
        ParticleBodyShapeSource _shapeSource;

        public ParticleBodyTopology Topology
        {
            get => _topology;
            set => _topology = value;
        }

        public Vector3 BaseDimensions
        {
            get => _baseDimensions;
            set => _baseDimensions = new Vector3(
                Mathf.Max(0.0001f, Mathf.Abs(value.x)),
                Mathf.Max(0.0001f, Mathf.Abs(value.y)),
                Mathf.Max(0.0001f, Mathf.Abs(value.z)));
        }

        public ParticleBodyShapeSource ShapeSource
        {
            get => _shapeSource;
            set => _shapeSource = value;
        }

        public override ParticleLayoutKind Kind => ParticleLayoutKind.Points;

        public override int VariantCount => _shapeSource != null ? _shapeSource.TemplateCount : 1;

        public override int GetParticleCount(int variant)
        {
            if (_shapeSource != null)
            {
                var scratch = new Vector3[Mathf.Max(12, _shapeSource.MaximumParticles)];
                _shapeSource.BuildTemplate(variant, _baseDimensions, scratch, out int count);
                return count;
            }
            return ParticleCount(_topology);
        }

        public override void GetRestPositions(int variant, List<Vector3> positions)
        {
            positions.Clear();
            var scratch = new Vector3[12];
            Build(variant, Vector3.one, scratch, out int count);
            for (int i = 0; i < count; i++)
                positions.Add(scratch[i]);
        }

        /// <summary>
        ///   The topology variant <paramref name="variant"/> realizes.
        /// </summary>
        public ParticleBodyTopology TopologyOf(int variant)
        {
            if (_shapeSource == null)
                return _topology;
            var scratch = new Vector3[Mathf.Max(12, _shapeSource.MaximumParticles)];
            return _shapeSource.BuildTemplate(variant, _baseDimensions, scratch, out _);
        }

        /// <summary>
        ///   Fills <paramref name="result"/> (at least 12 entries) with the variant's local rest
        ///   positions for a body scaled by <paramref name="scale"/>, and returns the topology realized.
        /// </summary>
        public ParticleBodyTopology Build(int variant, Vector3 scale, Vector3[] result, out int count)
        {
            Vector3 dimensions = Vector3.Scale(_baseDimensions, scale);
            if (_shapeSource != null)
                return _shapeSource.BuildTemplate(variant, dimensions, result, out count);
            count = BuildLocalShape(_topology, dimensions, result);
            return _topology;
        }

        /// <summary>
        ///   Particles of a topology.
        /// </summary>
        public static int ParticleCount(ParticleBodyTopology topology)
        {
            switch (topology)
            {
                case ParticleBodyTopology.Single: return 1;
                case ParticleBodyTopology.Chain3: return 3;
                case ParticleBodyTopology.GuideChain4: return 4;
                case ParticleBodyTopology.DualRail6: return 6;
                case ParticleBodyTopology.ArticulatedCluster12: return 12;
                default: return ParticleHullShapes.ClusterParticles(topology);
            }
        }

        /// <summary>
        ///   Local rest positions of <paramref name="topology"/> for a body of
        ///   <paramref name="dimensions"/> (already scaled), the port of
        ///   <c>SolverParticleEmitter.BuildLocalShape</c>. Returns the particle count.
        /// </summary>
        public static int BuildLocalShape(ParticleBodyTopology topology, Vector3 dimensions, Vector3[] result)
        {
            float hx = dimensions.x * 0.5f;
            float hy = dimensions.y * 0.5f;
            float hz = dimensions.z * 0.5f;

            switch (topology)
            {
                case ParticleBodyTopology.Single:
                    result[0] = Vector3.zero;
                    return 1;

                case ParticleBodyTopology.Chain3:
                    result[0] = Vector3.up * hy;
                    result[1] = Vector3.zero;
                    result[2] = Vector3.down * hy;
                    return 3;

                case ParticleBodyTopology.GuideChain4:
                    result[0] = Vector3.up * hy;
                    result[1] = Vector3.zero;
                    result[2] = Vector3.down * hy;
                    result[3] = Vector3.right * hx;
                    return 4;

                case ParticleBodyTopology.DualRail6:
                    result[0] = new Vector3(hx, hy, 0f);
                    result[1] = new Vector3(-hx, hy, 0f);
                    result[2] = new Vector3(hx, 0f, 0f);
                    result[3] = new Vector3(-hx, 0f, 0f);
                    result[4] = new Vector3(hx, -hy, 0f);
                    result[5] = new Vector3(-hx, -hy, 0f);
                    return 6;

                case ParticleBodyTopology.RigidCluster4:
                case ParticleBodyTopology.RigidCluster6:
                case ParticleBodyTopology.RigidCluster8:
                {
                    Vector3[] baseVertices = ParticleHullShapes.BaseVertices(topology);
                    var halfExtents = new Vector3(hx, hy, hz);
                    for (int i = 0; i < baseVertices.Length; i++)
                        result[i] = Vector3.Scale(baseVertices[i], halfExtents);
                    return baseVertices.Length;
                }

                case ParticleBodyTopology.ArticulatedCluster12:
                    BuildArticulatedShape(dimensions, result);
                    return 12;

                default:
                    return 0;
            }
        }

        static void BuildArticulatedShape(Vector3 dimensions, Vector3[] result)
        {
            float centerSpacing = dimensions.y / 3f;
            float segmentHalfLength = dimensions.y / 6f;
            float hx = dimensions.x * 0.5f;
            float hz = dimensions.z * 0.5f;
            for (int segment = 0; segment < 3; segment++)
            {
                float center = (1 - segment) * centerSpacing;
                int start = segment * 4;
                Vector3 centerOffset = Vector3.up * center;
                result[start] = centerOffset + new Vector3(hx, segmentHalfLength, hz);
                result[start + 1] = centerOffset + new Vector3(-hx, segmentHalfLength, -hz);
                result[start + 2] = centerOffset + new Vector3(-hx, -segmentHalfLength, hz);
                result[start + 3] = centerOffset + new Vector3(hx, -segmentHalfLength, -hz);
            }
        }

        public override void Validate(List<PhysicsParticleMessage> messages)
        {
            base.Validate(messages);
            if (_shapeSource == null && _topology == ParticleBodyTopology.Single)
                return;
            if (_shapeSource != null)
                for (int v = 0; v < VariantCount; v++)
                {
                    int count = GetParticleCount(v);
                    if (count != 4 && count != 6 && count != 8)
                        messages.Add(PhysicsParticleMessage.Error($"{name}: template {v} of {_shapeSource.name} has {count} particles; a rigid cluster has 4, 6 or 8."));
                }
        }

        void OnValidate()
        {
            BaseDimensions = _baseDimensions;
        }
    }
}
