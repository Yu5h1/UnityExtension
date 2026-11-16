// Motion BoneChain 1.0.0 (motion.bone-chain) implementation for Unity.
// Conforms strictly to Motion/Documentation/ModuleSpec/BoneChain.md.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    [Serializable]
    public struct BoneChainNode
    {
        public float Stiffness;
        public float Damping;
        public float Radius;

        public BoneChainNode(float stiffness, float damping, float radius)
        {
            Stiffness = stiffness;
            Damping = damping;
            Radius = radius;
        }
    }

    public enum BoneChainColliderType
    {
        Sphere,
        Capsule
    }

    [Serializable]
    public struct BoneChainCollider
    {
        public BoneChainColliderType Type;
        public Vector3 A;
        public Vector3 B;
        public float Radius;

        public static BoneChainCollider CreateSphere(Vector3 center, float radius)
        {
            return new BoneChainCollider
            {
                Type = BoneChainColliderType.Sphere,
                A = center,
                B = center,
                Radius = radius
            };
        }

        public static BoneChainCollider CreateCapsule(Vector3 a, Vector3 b, float radius)
        {
            return new BoneChainCollider
            {
                Type = BoneChainColliderType.Capsule,
                A = a,
                B = b,
                Radius = radius
            };
        }
    }

    /// <summary>
    /// Motion BoneChain 1.0.0 (motion.bone-chain) reference service for secondary character motion.
    /// Provides deterministic fixed-step damped springs, root-to-tip length constraints, and Sphere/Capsule geometric push-out.
    /// </summary>
    public sealed class MotionBoneChain
    {
        public const double Step = 1.0 / 120.0;
        public const int MaxSteps = 8;
        public const int MaxNodes = 64;
        public const int MaxIterations = 16;
        public const float MaxStiffness = 14400.0f;
        public const float MaxDamping = 120.0f;

        private readonly BoneChainNode[] _nodes;
        private readonly Vector3[] _pose;
        private readonly Vector3[] _position;
        private readonly Vector3[] _velocity;
        private readonly List<BoneChainCollider> _colliders;

        private Vector3 _gravity = Vector3.zero;
        private int _iterations = 1;
        private double _acc = 0.0;
        private bool _initialized = false;

        public int Nodes => _nodes.Length;
        public bool Initialized => _initialized;
        public Vector3 Gravity => _gravity;
        public int Iterations => _iterations;
        public IReadOnlyList<BoneChainCollider> Colliders => _colliders;

        public MotionBoneChain(int nodes)
        {
            if (nodes < 1 || nodes > MaxNodes)
            {
                throw new ArgumentOutOfRangeException(nameof(nodes), $"Nodes count must be between 1 and {MaxNodes}.");
            }

            _nodes = new BoneChainNode[nodes];
            _pose = new Vector3[nodes + 1];
            _position = new Vector3[nodes + 1];
            _velocity = new Vector3[nodes + 1];
            _colliders = new List<BoneChainCollider>();
        }

        public static MotionBoneChain TryCreate(int nodes)
        {
            if (nodes < 1 || nodes > MaxNodes) return null;
            return new MotionBoneChain(nodes);
        }

        public bool SetNode(int nodeIndex, float stiffness, float damping, float radius)
        {
            if (nodeIndex < 0 || nodeIndex >= Nodes) return false;
            if (!(stiffness >= 0f && stiffness <= MaxStiffness) ||
                !(damping >= 0f && damping <= MaxDamping) ||
                !(radius >= 0f && !float.IsNaN(radius) && !float.IsInfinity(radius)))
            {
                return false;
            }

            _nodes[nodeIndex] = new BoneChainNode(stiffness, damping, radius);
            return true;
        }

        public BoneChainNode GetNode(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= Nodes)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeIndex));
            }
            return _nodes[nodeIndex];
        }

        public bool SetGravity(Vector3 gravity)
        {
            if (IsInvalidVector(gravity)) return false;
            _gravity = gravity;
            return true;
        }

        public bool SetIterations(int iterations)
        {
            if (iterations < 1 || iterations > MaxIterations) return false;
            _iterations = iterations;
            return true;
        }

        public bool SetJoint(int jointIndex, Vector3 position)
        {
            if (jointIndex < 0 || jointIndex > Nodes) return false;
            _pose[jointIndex] = position;
            return true;
        }

        public void ClearColliders()
        {
            _colliders.Clear();
        }

        public bool AddSphere(Vector3 center, float radius)
        {
            if (IsInvalidVector(center) || !(radius >= 0f && !float.IsNaN(radius) && !float.IsInfinity(radius)))
            {
                return false;
            }
            _colliders.Add(BoneChainCollider.CreateSphere(center, radius));
            return true;
        }

        public bool AddCapsule(Vector3 a, Vector3 b, float radius)
        {
            if (IsInvalidVector(a) || IsInvalidVector(b) || !(radius >= 0f && !float.IsNaN(radius) && !float.IsInfinity(radius)))
            {
                return false;
            }
            _colliders.Add(BoneChainCollider.CreateCapsule(a, b, radius));
            return true;
        }

        public void Reset()
        {
            _initialized = false;
            _acc = 0.0;
            for (int i = 0; i < _velocity.Length; i++)
            {
                _velocity[i] = Vector3.zero;
            }
        }

        public int Tick(double dt)
        {
            if (!(dt > 0.0) || double.IsNaN(dt) || double.IsInfinity(dt)) return -1;
            for (int j = 0; j <= Nodes; j++)
            {
                if (IsInvalidVector(_pose[j])) return -1;
            }

            if (!_initialized)
            {
                Array.Copy(_pose, _position, _pose.Length);
                for (int i = 0; i < _velocity.Length; i++)
                {
                    _velocity[i] = Vector3.zero;
                }
                _acc = 0.0;
                _initialized = true;
            }

            _acc += dt;
            int steps = (int)Math.Floor((_acc + 1e-9) / Step);
            if (steps > MaxSteps)
            {
                steps = MaxSteps;
                _acc = 0.0;
            }
            else
            {
                _acc -= steps * Step;
                if (_acc < 0.0) _acc = 0.0;
            }

            for (int s = 0; s < steps; s++)
            {
                Advance();
            }

            return steps;
        }

        public Vector3 Position(int jointIndex)
        {
            if (jointIndex < 0 || jointIndex > Nodes) return Vector3.zero;
            return jointIndex == 0 ? _pose[0] : _position[jointIndex];
        }

        public Quaternion Aim(int boneIndex)
        {
            if (boneIndex < 1 || boneIndex > Nodes) return Quaternion.identity;

            Vector3 bone = _pose[boneIndex] - _pose[boneIndex - 1];
            float len = bone.magnitude;

            Vector3 pCurr = Position(boneIndex);
            Vector3 pPrev = Position(boneIndex - 1);
            Vector3 sim = pCurr - pPrev;
            float simLen = sim.magnitude;

            if (!(len > 0f && simLen > 0f)) return Quaternion.identity;

            Vector3 u = bone / len;
            Vector3 w = sim / simLen;
            float d = 1.0f + Vector3.Dot(u, w);

            if (d < 1e-6f)
            {
                Vector3 axis = Vector3.Cross(u, new Vector3(1f, 0f, 0f));
                if (axis.sqrMagnitude == 0f) axis = Vector3.Cross(u, new Vector3(0f, 1f, 0f));
                axis.Normalize();
                return new Quaternion(axis.x, axis.y, axis.z, 0f);
            }

            Vector3 c = Vector3.Cross(u, w);
            float n = Mathf.Sqrt(Vector3.Dot(c, c) + d * d);
            return new Quaternion(c.x / n, c.y / n, c.z / n, d / n);
        }

        public float Read(int jointIndex, int field)
        {
            if (field == 7) return _initialized ? 1.0f : 0.0f;
            if (jointIndex < 0 || jointIndex > Nodes) return 0f;

            if (field >= 0 && field <= 2)
            {
                Vector3 pos = Position(jointIndex);
                return field == 0 ? pos.x : field == 1 ? pos.y : pos.z;
            }

            if (field >= 3 && field <= 6)
            {
                if (jointIndex < 1) return 0f;
                Quaternion rot = Aim(jointIndex);
                return field == 3 ? rot.x : field == 4 ? rot.y : field == 5 ? rot.z : rot.w;
            }

            return 0f;
        }

        private void Advance()
        {
            float h = (float)Step;
            int n = Nodes;
            Vector3[] q = new Vector3[n + 1];
            q[0] = _pose[0];

            for (int i = 1; i <= n; i++)
            {
                BoneChainNode node = _nodes[i - 1];
                Vector3 head = i == 1 ? _pose[0] : _position[i - 1];
                Vector3 target = head + (_pose[i] - _pose[i - 1]);
                Vector3 accel = (target - _position[i]) * node.Stiffness - _velocity[i] * node.Damping + _gravity;
                Vector3 v = _velocity[i] + accel * h;
                q[i] = _position[i] + v * h;
            }

            for (int it = 0; it < _iterations; it++)
            {
                for (int i = 1; i <= n; i++)
                {
                    PushOut(ref q[i], i);
                    float len = (_pose[i] - _pose[i - 1]).magnitude;
                    if (!(len > 0f))
                    {
                        q[i] = q[i - 1];
                        continue;
                    }

                    Vector3 d = q[i] - q[i - 1];
                    float dl = d.magnitude;
                    Vector3 dir = dl > 0f ? d / dl : Direction(i);
                    q[i] = q[i - 1] + dir * len;
                }
            }

            for (int i = 1; i <= n; i++)
            {
                _velocity[i] = (q[i] - _position[i]) / h;
                _position[i] = q[i];
            }
        }

        private void PushOut(ref Vector3 q, int boneIndex)
        {
            float r = _nodes[boneIndex - 1].Radius;
            for (int cIdx = 0; cIdx < _colliders.Count; cIdx++)
            {
                BoneChainCollider col = _colliders[cIdx];
                Vector3 c = col.A;
                Vector3 ab = col.B - col.A;
                float ab2 = Vector3.Dot(ab, ab);
                if (ab2 > 0f)
                {
                    float t = Vector3.Dot(q - col.A, ab) / ab2;
                    if (t < 0f) t = 0f;
                    else if (t > 1f) t = 1f;
                    c = col.A + ab * t;
                }

                Vector3 d = q - c;
                float dist = d.magnitude;
                float reach = col.Radius + r;
                if (dist < reach)
                {
                    Vector3 dir = dist > 0f ? d / dist : Direction(boneIndex);
                    q = c + dir * reach;
                }
            }
        }

        private Vector3 Direction(int boneIndex)
        {
            Vector3 bone = _pose[boneIndex] - _pose[boneIndex - 1];
            float len = bone.magnitude;
            return len > 0f ? bone / len : new Vector3(0f, 1f, 0f);
        }

        private static bool IsInvalidVector(Vector3 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                   float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);
        }
    }
}
