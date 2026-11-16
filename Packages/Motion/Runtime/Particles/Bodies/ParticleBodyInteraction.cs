using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   One body to create: where it is, how it is turned and scaled, and how it moves
    ///   (ParticleExtensions EXT-11). Mirrors Unified-Solver's SolverParticleSpawnRequest.
    ///   <see cref="scale"/> multiplies the layout's base dimensions; <see cref="color"/> with alpha 0
    ///   means the appearance's own colour.
    /// </summary>
    [System.Serializable]
    public struct ParticleBodySpawn
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public Vector3 scale;
        public Color color;

        public static ParticleBodySpawn Create(Vector3 position, Vector3 velocity, Color color) => new ParticleBodySpawn
        {
            position = position,
            rotation = Quaternion.identity,
            velocity = velocity,
            angularVelocity = Vector3.zero,
            scale = Vector3.one,
            color = color
        };
    }

    /// <summary>
    ///   The physics of a <see cref="ParticleBodyLayout"/> body: its mass, particle radius and the
    ///   joint and bend constraints its topology gets (ParticleExtensions EXT-10). The mass is the
    ///   whole body's, divided evenly over its particles as in Unified-Solver, so a 4 particle
    ///   fragment is denser per particle than an 8 particle one; the inherited per-particle mass
    ///   is ignored. Joints use <see cref="JointCompliance"/> and <see cref="JointDamping"/>, bends
    ///   use <see cref="BendCompliance"/> and <see cref="JointDamping"/>; no constraint ever breaks.
    /// </summary>
    public sealed class ParticleBodyInteraction : PhysicsParticleInteraction
    {
        [Tooltip("Mass of the whole body, in the profile's mass unit, shared evenly between its particles.")]
        [Min(0.0001f)]
        [SerializeField]
        float _bodyMass = 1f;
        [Min(0f)]
        [SerializeField]
        float _jointCompliance = 0.00002f;
        [Min(0f)]
        [SerializeField]
        float _bendCompliance = 0.0005f;
        [Min(0f)]
        [SerializeField]
        float _jointDamping = 0.15f;

        public float BodyMass
        {
            get => _bodyMass;
            set => _bodyMass = Mathf.Max(0.0001f, value);
        }

        public float JointCompliance
        {
            get => _jointCompliance;
            set => _jointCompliance = Mathf.Max(0f, value);
        }

        public float BendCompliance
        {
            get => _bendCompliance;
            set => _bendCompliance = Mathf.Max(0f, value);
        }

        public float JointDamping
        {
            get => _jointDamping;
            set => _jointDamping = Mathf.Max(0f, value);
        }

        public override bool Accepts(ParticleLayoutKind kind) => kind == ParticleLayoutKind.Points;

        public override void Build(PhysicsParticleLayout layout, int variant, ParticleEntityRequest request)
        {
            var bodyLayout = layout as ParticleBodyLayout;
            if (bodyLayout == null)
                return;
            ApplyMass(request);
            AddConstraints(bodyLayout.TopologyOf(variant), request);
        }

        /// <summary>
        ///   Builds the request for one body: particles placed by the layout at the spawn's scale,
        ///   given the spawn's velocity (plus its angular velocity about the body origin), the body's
        ///   mass, constraints and rigid groups. The entity pose is the spawn's position and rotation.
        /// </summary>
        public ParticleEntityRequest BuildBody(ParticleBodyLayout layout, int variant, in ParticleBodySpawn spawn, uint category)
        {
            var request = new ParticleEntityRequest
            {
                pose = new Pose(spawn.position, spawn.rotation),
                category = category,
                groupMode = SelfCollision ? ParticleGroupMode.Shared : ParticleGroupMode.NewGroup
            };
            var shape = new Vector3[12];
            ParticleBodyTopology topology = layout.Build(variant, spawn.scale, shape, out int count);
            Quaternion inverse = Quaternion.Inverse(spawn.rotation);
            for (int i = 0; i < count; i++)
            {
                Vector3 worldOffset = spawn.rotation * shape[i];
                Vector3 worldVelocity = spawn.velocity + Vector3.Cross(spawn.angularVelocity, worldOffset);
                request.particles.Add(new ParticleSpawn
                {
                    position = shape[i],
                    velocity = inverse * worldVelocity,
                    radius = ParticleRadius
                });
                request.roles.Add(new Vector3Int(i, 0, 0));
            }
            ApplyMass(request);
            AddConstraints(topology, request);
            return request;
        }

        void ApplyMass(ParticleEntityRequest request)
        {
            float particleMass = _bodyMass / Mathf.Max(1, request.particles.Count);
            float invMass = particleMass > 0f ? 1f / particleMass : 0f;
            for (int i = 0; i < request.particles.Count; i++)
            {
                ParticleSpawn spawn = request.particles[i];
                spawn.invMass = invMass;
                request.particles[i] = spawn;
            }
        }

        /// <summary>
        ///   Adds the constraints and rigid groups of <paramref name="topology"/>, the port of
        ///   <c>SolverParticleEmitter.AddTopologyData</c>.
        /// </summary>
        public void AddConstraints(ParticleBodyTopology topology, ParticleEntityRequest request)
        {
            switch (topology)
            {
                case ParticleBodyTopology.Chain3:
                    Joint(request, 0, 1);
                    Joint(request, 1, 2);
                    Bend(request, 0, 2);
                    break;

                case ParticleBodyTopology.GuideChain4:
                    Joint(request, 0, 1);
                    Joint(request, 1, 2);
                    Joint(request, 1, 3);
                    Bend(request, 0, 3);
                    Bend(request, 2, 3);
                    Bend(request, 0, 2);
                    break;

                case ParticleBodyTopology.DualRail6:
                    Joint(request, 0, 2);
                    Joint(request, 2, 4);
                    Joint(request, 1, 3);
                    Joint(request, 3, 5);
                    Joint(request, 0, 1);
                    Joint(request, 2, 3);
                    Joint(request, 4, 5);
                    Bend(request, 0, 3);
                    Bend(request, 1, 2);
                    Bend(request, 2, 5);
                    Bend(request, 3, 4);
                    Bend(request, 0, 4);
                    Bend(request, 1, 5);
                    break;

                case ParticleBodyTopology.RigidCluster4:
                case ParticleBodyTopology.RigidCluster6:
                case ParticleBodyTopology.RigidCluster8:
                    request.rigidGroups.Add(Members(0, ParticleHullShapes.ClusterParticles(topology)));
                    break;

                case ParticleBodyTopology.ArticulatedCluster12:
                    for (int segment = 0; segment < 3; segment++)
                        request.rigidGroups.Add(Members(segment * 4, 4));
                    ArticulatedJoint(request, 0, 1);
                    ArticulatedJoint(request, 1, 2);
                    break;
            }
        }

        void ArticulatedJoint(ParticleEntityRequest request, int frontSegment, int rearSegment)
        {
            int front = frontSegment * 4;
            int rear = rearSegment * 4;
            Joint(request, front + 2, rear);
            Joint(request, front + 3, rear + 1);
            Bend(request, front + 2, rear + 1);
            Bend(request, front + 3, rear);
            Bend(request, front, rear + 2);
            Bend(request, front + 1, rear + 3);
        }

        static int[] Members(int start, int count)
        {
            var members = new int[count];
            for (int i = 0; i < count; i++)
                members[i] = start + i;
            return members;
        }

        void Joint(ParticleEntityRequest request, int a, int b) =>
            request.constraints.Add(Spec(a, b, _jointCompliance));

        void Bend(ParticleEntityRequest request, int a, int b) =>
            request.constraints.Add(Spec(a, b, _bendCompliance));

        DistanceConstraintSpec Spec(int a, int b, float compliance) => new DistanceConstraintSpec
        {
            a = a,
            b = b,
            restLength = float.NaN,
            compliance = compliance,
            damping = _jointDamping,
            breakForce = 0f
        };

        public override void Validate(PhysicsParticleLayout layout, List<PhysicsParticleMessage> messages)
        {
            base.Validate(layout, messages);
            if (layout != null && !(layout is ParticleBodyLayout))
                messages.Add(PhysicsParticleMessage.Error($"{name} needs a ParticleBodyLayout, not {layout.GetType().Name}."));
        }
    }
}
