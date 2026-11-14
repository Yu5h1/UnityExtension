using UnityEngine;

namespace Yu5h1Lib.UnifiedSolver
{
    /// <summary>
    ///   An oriented box that pushes only the particles whose source GameObject
    ///   is in a layer this collider collides with.
    /// </summary>
    /// <remarks>
    ///   The box is the Transform's, exactly as the vendored SolverBoxCollider
    ///   reads it: centre at the position, half extents of half the lossy scale,
    ///   axes of the rotation. A particle's layer is the layer of the GameObject
    ///   of the body that owns it -- the emitter, or the cloth or rope beside its
    ///   SolverClothParticles / SolverRopeParticles -- so to make something pass
    ///   through this box, put that object in a layer and leave the layer out of
    ///   <see cref="CollideWith"/>. Particles no such body owns count as Default.
    ///
    ///   It runs once per FixedUpdate, after the solver's step, not inside its
    ///   substeps (the solver has no hook there and is not modified). So a body
    ///   that gets deep into a thin box in one step comes out of the nearest
    ///   face, which is not always the side it came from; keep the box thick
    ///   enough for what moves into it, or tune <see cref="friction"/>.
    ///   A box moves what it touches along with it: its own velocity, and its
    ///   turn about its centre, are given to the particles it pushes.
    /// </remarks>
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public sealed class SolverLayerBoxCollider : MonoBehaviour
    {
        const string ComputeResource = "SolverLayerBox";
        const int ThreadsPerGroup = 64;

        [Tooltip("The layers of particle sources this box pushes. A source in a layer left out passes straight through. Everything makes it behave like SolverBoxCollider.")]
        [SerializeField] LayerMask collideWith = ~0;

        [Tooltip("The share of a particle's sliding speed along the surface that touching it removes in one step: 0 = frictionless, 1 = it sticks to the surface and goes where the box goes.")]
        [Range(0f, 1f)]
        [SerializeField] float friction = 0f;

        ComputeShader _compute;
        int _kernel = -1;
        bool _reportedMissingCompute;
        bool _hasPreviousPose;
        Vector3 _previousCenter;
        Quaternion _previousRotation;
        Vector3 _velocity;
        Vector3 _angularVelocity;

        /// <summary>The layers this box pushes particles of.</summary>
        public LayerMask CollideWith
        {
            get => collideWith;
            set => collideWith = value;
        }

        /// <summary>Share of the sliding speed removed per step.</summary>
        public float Friction
        {
            get => friction;
            set => friction = Mathf.Clamp01(value);
        }

        public Vector3 WorldCenter => transform.position;
        public Vector3 HalfExtents => transform.lossyScale * 0.5f;
        public Vector3 WorldAxisX => transform.right;
        public Vector3 WorldAxisY => transform.up;
        public Vector3 WorldAxisZ => transform.forward;

        void OnEnable()
        {
            _hasPreviousPose = false;
        }

        void FixedUpdate()
        {
            if (collideWith.value == 0)
                return;

            UpdateMotion();

            SolverManager manager = SolverManager.Instance;
            if (manager == null ||
                manager.ActiveCount <= 0 ||
                manager.ParticleBuffer == null ||
                !manager.ParticleBuffer.IsValid())
            {
                return;
            }

            if (!TryEnsureCompute())
                return;

            ComputeBuffer layers = SolverParticleLayers.Resolve(manager);
            if (layers == null)
                return;

            Vector3 half = HalfExtents;
            _compute.SetBuffer(_kernel, "_Particles", manager.ParticleBuffer);
            _compute.SetBuffer(_kernel, "_ParticleLayers", layers);
            _compute.SetInt("_ParticleCount", manager.ActiveCount);
            _compute.SetFloat("_ParticleRadius", manager.particleRadius);
            _compute.SetInt("_LayerMask", collideWith.value);
            _compute.SetFloat("_Friction", friction);
            _compute.SetVector("_BoxCenter", WorldCenter);
            _compute.SetVector(
                "_BoxHalfExtents",
                new Vector3(
                    Mathf.Abs(half.x),
                    Mathf.Abs(half.y),
                    Mathf.Abs(half.z)));
            _compute.SetVector("_BoxAxisX", WorldAxisX);
            _compute.SetVector("_BoxAxisY", WorldAxisY);
            _compute.SetVector("_BoxAxisZ", WorldAxisZ);
            _compute.SetVector("_BoxVelocity", _velocity);
            _compute.SetVector("_BoxAngularVelocity", _angularVelocity);
            _compute.Dispatch(
                _kernel,
                Mathf.CeilToInt(manager.ActiveCount / (float)ThreadsPerGroup),
                1,
                1);
        }

        bool TryEnsureCompute()
        {
            if (_compute != null)
                return true;

            _compute = Resources.Load<ComputeShader>(ComputeResource);
            if (_compute == null)
            {
                if (!_reportedMissingCompute)
                {
                    Debug.LogError(
                        "SolverLayerBoxCollider: could not load Resources/" +
                        ComputeResource + ".compute.",
                        this);
                    _reportedMissingCompute = true;
                }
                return false;
            }

            _kernel = _compute.FindKernel("SolveLayerBox");
            return true;
        }

        // The box's own motion since the previous step, so what it pushes can be
        // given its velocity. A box that was just enabled has none.
        void UpdateMotion()
        {
            Vector3 center = WorldCenter;
            Quaternion rotation = transform.rotation;
            float dt = Time.fixedDeltaTime;

            if (!_hasPreviousPose || dt <= 0f)
            {
                _velocity = Vector3.zero;
                _angularVelocity = Vector3.zero;
                _hasPreviousPose = true;
            }
            else
            {
                _velocity = (center - _previousCenter) / dt;
                _angularVelocity = AngularVelocity(
                    _previousRotation, rotation, 1f / dt);
            }

            _previousCenter = center;
            _previousRotation = rotation;
        }

        static Vector3 AngularVelocity(
            Quaternion previous,
            Quaternion current,
            float inverseDeltaTime)
        {
            Quaternion delta = current * Quaternion.Inverse(previous);
            if (delta.w < 0f)
            {
                delta.x = -delta.x;
                delta.y = -delta.y;
                delta.z = -delta.z;
                delta.w = -delta.w;
            }

            delta.ToAngleAxis(out float degrees, out Vector3 axis);
            if (degrees > 180f)
                degrees -= 360f;

            if (Mathf.Abs(degrees) < 0.0001f ||
                float.IsNaN(axis.x) ||
                float.IsNaN(axis.y) ||
                float.IsNaN(axis.z))
            {
                return Vector3.zero;
            }

            return axis.normalized *
                (degrees * Mathf.Deg2Rad * inverseDeltaTime);
        }

        void OnDrawGizmos()
        {
            DrawBox(
                new Color(0.2f, 0.8f, 1f, 0.12f),
                new Color(0.2f, 0.8f, 1f, 0.6f));
        }

        void OnDrawGizmosSelected()
        {
            DrawBox(
                new Color(0.3f, 0.9f, 1f, 0.25f),
                new Color(0.3f, 0.9f, 1f, 1f));
        }

        void DrawBox(Color fill, Color wire)
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                transform.lossyScale);

            Gizmos.color = fill;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.color = wire;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = previous;
        }
    }
}
