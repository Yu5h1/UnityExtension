// BoneChainComponent (Unity presentation adapter for motion.bone-chain 1.0.0).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    [Serializable]
    public class BoneChainNodeSetting
    {
        public Transform BoneTransform;
        [Range(0f, 14400f)] public float Stiffness = 80f;
        [Range(0f, 120f)] public float Damping = 6f;
        [Min(0f)] public float Radius = 0.03f;
    }

    [Serializable]
    public class BoneChainSphereColliderConfig
    {
        public Transform Anchor;
        public Vector3 CenterOffset;
        [Min(0f)] public float Radius = 0.15f;
    }

    [Serializable]
    public class BoneChainCapsuleColliderConfig
    {
        public Transform Anchor;
        public Vector3 StartOffset;
        public Vector3 EndOffset;
        [Min(0f)] public float Radius = 0.12f;
    }

    /// <summary>
    /// Unity component applying Motion BoneChain secondary physics to a hierarchy of bones
    /// (hair, ponytails, skirts, capes, ribbons) with anti-clipping push-out collision.
    /// Executes in LateUpdate after Animator evaluation.
    /// </summary>
    [AddComponentMenu("Yu5h1Lib/Motion/Bone Chain Component")]
    public sealed class BoneChainComponent : MonoBehaviour
    {
        [Header("Bone Chain Hierarchy")]
        [Tooltip("The parent/root transform driven purely by animation (e.g. Head or Pelvis).")]
        [SerializeField] private Transform _chainRoot;

        [Tooltip("Ordered list of bone segments from root to tip.")]
        [SerializeField] private List<BoneChainNodeSetting> _nodes = new List<BoneChainNodeSetting>();

        [Header("Physics Parameters")]
        [SerializeField] private Vector3 _gravity = new Vector3(0f, -9.81f, 0f);
        [Range(1, 16)] [SerializeField] private int _iterations = 3;

        [Header("Anti-Clipping Colliders")]
        [SerializeField] private List<SphereCollider> _unitySphereColliders = new List<SphereCollider>();
        [SerializeField] private List<CapsuleCollider> _unityCapsuleColliders = new List<CapsuleCollider>();
        [SerializeField] private List<BoneChainSphereColliderConfig> _customSphereColliders = new List<BoneChainSphereColliderConfig>();
        [SerializeField] private List<BoneChainCapsuleColliderConfig> _customCapsuleColliders = new List<BoneChainCapsuleColliderConfig>();

        private MotionBoneChain _chain;
        private bool _isConfigured = false;

        public MotionBoneChain CoreChain => _chain;
        public Transform ChainRoot { get => _chainRoot; set { _chainRoot = value; _isConfigured = false; } }
        public List<BoneChainNodeSetting> Nodes => _nodes;
        public List<SphereCollider> UnitySphereColliders => _unitySphereColliders;
        public List<CapsuleCollider> UnityCapsuleColliders => _unityCapsuleColliders;

        private void OnEnable()
        {
            InitializeChain();
        }

        private void OnDisable()
        {
            _isConfigured = false;
        }

        public void InitializeChain()
        {
            if (_chainRoot == null || _nodes == null || _nodes.Count == 0)
            {
                _isConfigured = false;
                return;
            }

            int count = Mathf.Clamp(_nodes.Count, 1, MotionBoneChain.MaxNodes);
            _chain = new MotionBoneChain(count);
            _chain.SetGravity(_gravity);
            _chain.SetIterations(_iterations);

            for (int i = 0; i < count; i++)
            {
                var setting = _nodes[i];
                _chain.SetNode(i, setting.Stiffness, setting.Damping, setting.Radius);
            }

            _isConfigured = true;
            ResetSimulation();
        }

        public void ResetSimulation()
        {
            if (!_isConfigured || _chain == null) return;

            // Capture initial animated pose
            _chain.SetJoint(0, _chainRoot.position);
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].BoneTransform != null)
                {
                    _chain.SetJoint(i + 1, _nodes[i].BoneTransform.position);
                }
            }

            _chain.Reset();
        }

        private void LateUpdate()
        {
            StepSimulation(Time.deltaTime);
        }

        /// <summary>
        /// Steps the secondary motion simulation explicitly. Safe to call in EditMode or PlayMode.
        /// </summary>
        public void StepSimulation(float dt)
        {
            if (!_isConfigured || _chain == null) return;

            // 1. Update pose from animation
            _chain.SetJoint(0, _chainRoot.position);
            for (int i = 0; i < _nodes.Count; i++)
            {
                Transform bone = _nodes[i].BoneTransform;
                if (bone != null)
                {
                    _chain.SetJoint(i + 1, bone.position);
                }
            }

            // 2. Gather active colliders
            _chain.ClearColliders();
            CollectUnityColliders();
            CollectCustomColliders();

            // 3. Step physics simulation
            if (dt <= 0f) return;

            int steps = _chain.Tick(dt);
            if (steps <= 0) return;

            // 4. Write back aim rotation and positions over animated pose
            for (int i = 0; i < _nodes.Count; i++)
            {
                Transform bone = _nodes[i].BoneTransform;
                if (bone == null) continue;

                int jointIdx = i + 1;
                Quaternion aimRotation = _chain.Aim(jointIdx);
                bone.rotation = aimRotation * bone.rotation;
                bone.position = _chain.Position(jointIdx);
            }
        }

        private void CollectUnityColliders()
        {
            for (int i = 0; i < _unitySphereColliders.Count; i++)
            {
                SphereCollider sc = _unitySphereColliders[i];
                if (sc == null || !sc.enabled || !sc.gameObject.activeInHierarchy) continue;

                Vector3 center = sc.transform.TransformPoint(sc.center);
                Vector3 scale = sc.transform.lossyScale;
                float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                _chain.AddSphere(center, sc.radius * maxScale);
            }

            for (int i = 0; i < _unityCapsuleColliders.Count; i++)
            {
                CapsuleCollider cc = _unityCapsuleColliders[i];
                if (cc == null || !cc.enabled || !cc.gameObject.activeInHierarchy) continue;

                Vector3 scale = cc.transform.lossyScale;
                float radiusScale = 1f;
                float heightScale = 1f;

                Vector3 dirVec = Vector3.up;
                if (cc.direction == 0) // X-Axis
                {
                    dirVec = Vector3.right;
                    heightScale = Mathf.Abs(scale.x);
                    radiusScale = Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                }
                else if (cc.direction == 1) // Y-Axis
                {
                    dirVec = Vector3.up;
                    heightScale = Mathf.Abs(scale.y);
                    radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                }
                else // Z-Axis
                {
                    dirVec = Vector3.forward;
                    heightScale = Mathf.Abs(scale.z);
                    radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                }

                float halfHeight = Mathf.Max(0f, (cc.height * 0.5f) - cc.radius) * heightScale;
                Vector3 worldCenter = cc.transform.TransformPoint(cc.center);
                Vector3 worldAxis = cc.transform.TransformDirection(dirVec).normalized;

                Vector3 pointA = worldCenter + worldAxis * halfHeight;
                Vector3 pointB = worldCenter - worldAxis * halfHeight;
                float radius = cc.radius * radiusScale;

                _chain.AddCapsule(pointA, pointB, radius);
            }
        }

        private void CollectCustomColliders()
        {
            for (int i = 0; i < _customSphereColliders.Count; i++)
            {
                var c = _customSphereColliders[i];
                Vector3 center = c.Anchor != null ? c.Anchor.TransformPoint(c.CenterOffset) : c.CenterOffset;
                _chain.AddSphere(center, c.Radius);
            }

            for (int i = 0; i < _customCapsuleColliders.Count; i++)
            {
                var c = _customCapsuleColliders[i];
                Vector3 a = c.Anchor != null ? c.Anchor.TransformPoint(c.StartOffset) : c.StartOffset;
                Vector3 b = c.Anchor != null ? c.Anchor.TransformPoint(c.EndOffset) : c.EndOffset;
                _chain.AddCapsule(a, b, c.Radius);
            }
        }
    }
}
