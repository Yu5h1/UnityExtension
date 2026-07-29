using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1.UnifiedSolver
{
    [RequireComponent(typeof(SolverParticleEmitter))]
    public sealed class SolverMeshRenderer : MonoBehaviour
    {
        const string RigidShaderName =
            "Yu5h1/UnifiedSolver/RigidMesh";
        const string ArticulatedShaderName =
            "Yu5h1/UnifiedSolver/ArticulatedMesh";

        [Min(1f)]
        public float drawBoundsSize = 1000f;

        SolverParticleEmitter _emitter;
        Material _runtimeMaterial;
        MaterialPropertyBlock _properties;
        SolverRenderProfile _activeRenderProfile;
        bool _reportedMissingSetup;

        void Awake()
        {
            _emitter =
                GetComponent<SolverParticleEmitter>();
            _properties =
                new MaterialPropertyBlock();
        }

        void Update()
        {
            if (!TryResolveSetup(
                    out SolverParticleProfile profile,
                    out SolverRenderProfile renderProfile))
            {
                return;
            }

            if (_runtimeMaterial == null ||
                _activeRenderProfile != renderProfile)
            {
                CreateRuntimeMaterial(renderProfile);
            }

            if (_runtimeMaterial == null ||
                _emitter.InstanceCount == 0 ||
                _emitter.InstanceBuffer == null ||
                _emitter.Solver == null ||
                _emitter.Solver.ParticleBuffer == null)
            {
                return;
            }

            Mesh mesh = renderProfile.mesh;
            Vector3 baseVisualScale =
                GetBaseVisualScale(
                    mesh,
                    profile.baseDimensions,
                    renderProfile);
            Bounds meshBounds = mesh.bounds;

            _properties.Clear();
            _properties.SetBuffer(
                "_Particles",
                _emitter.Solver.ParticleBuffer);
            _properties.SetBuffer(
                "_Instances",
                _emitter.InstanceBuffer);
            if (!TryBindRigidBuffers(renderProfile))
                return;

            _properties.SetVector(
                "_MeshCenter",
                meshBounds.center);
            _properties.SetVector(
                "_BaseVisualScale",
                baseVisualScale);
            _properties.SetVector(
                "_BaseDimensions",
                profile.baseDimensions);
            _properties.SetInt(
                "_MeshForwardAxis",
                (int)renderProfile.forwardAxis);
            _properties.SetFloat(
                "_MeshAxisMin",
                AxisComponent(
                    meshBounds.min,
                    renderProfile.forwardAxis));
            _properties.SetFloat(
                "_MeshAxisLength",
                Mathf.Max(
                    0.000001f,
                    AxisComponent(
                        meshBounds.size,
                        renderProfile.forwardAxis)));

            Bounds drawBounds = new Bounds(
                transform.position,
                Vector3.one * drawBoundsSize);
            ShadowCastingMode shadows =
                renderProfile.castShadows
                    ? ShadowCastingMode.On
                    : ShadowCastingMode.Off;

            Graphics.DrawMeshInstancedProcedural(
                mesh,
                0,
                _runtimeMaterial,
                drawBounds,
                _emitter.InstanceCount,
                _properties,
                shadows,
                renderProfile.receiveShadows,
                gameObject.layer);
        }

        bool TryBindRigidBuffers(
            SolverRenderProfile renderProfile)
        {
            if (renderProfile.meshMode !=
                SolverMeshMode.Rigid)
            {
                return true;
            }

            if (!SolverManagerAccess.TryGetRigidBuffers(
                    _emitter.Solver,
                    out ComputeBuffer rigidBodyBuffer,
                    out ComputeBuffer
                        rigidParticleIndexBuffer))
            {
                return false;
            }

            if (rigidBodyBuffer == null ||
                rigidParticleIndexBuffer == null)
            {
                return false;
            }

            _properties.SetBuffer(
                "_RigidBodies",
                rigidBodyBuffer);
            _properties.SetBuffer(
                "_RigidParticleIndices",
                rigidParticleIndexBuffer);
            return true;
        }

        bool TryResolveSetup(
            out SolverParticleProfile profile,
            out SolverRenderProfile renderProfile)
        {
            profile = _emitter != null
                ? _emitter.profile
                : null;
            renderProfile = profile != null
                ? profile.renderProfile
                : null;

            bool valid =
                profile != null &&
                renderProfile != null &&
                renderProfile.mesh != null;
            if (!valid && !_reportedMissingSetup)
            {
                Debug.LogWarning(
                    "SolverMeshRenderer: Assign a Particle " +
                    "Profile with a Render Profile and Mesh.",
                    this);
                _reportedMissingSetup = true;
            }
            if (valid)
                _reportedMissingSetup = false;
            return valid;
        }

        void CreateRuntimeMaterial(
            SolverRenderProfile renderProfile)
        {
            DestroyRuntimeMaterial();
            Shader shader =
                renderProfile.meshMode ==
                SolverMeshMode.Rigid
                    ? renderProfile.rigidShader
                    : renderProfile.articulatedShader;
            if (shader == null)
            {
                shader = Shader.Find(
                    renderProfile.meshMode ==
                    SolverMeshMode.Rigid
                        ? RigidShaderName
                        : ArticulatedShaderName);
            }

            if (shader == null)
            {
                Debug.LogError(
                    "SolverMeshRenderer: Compatible shader " +
                    "not found. Assign it on Render Profile.",
                    this);
                return;
            }

            _runtimeMaterial = new Material(shader)
            {
                name =
                    $"{shader.name} (Runtime)"
            };
            _activeRenderProfile = renderProfile;
            CopyMaterialProperties(
                renderProfile.sourceMaterial,
                _runtimeMaterial);
        }

        static void CopyMaterialProperties(
            Material source,
            Material destination)
        {
            if (source == null ||
                destination == null)
            {
                return;
            }

            Texture texture = null;
            if (source.HasProperty("_BaseMap"))
                texture = source.GetTexture("_BaseMap");
            if (texture == null &&
                source.HasProperty("_MainTex"))
            {
                texture = source.GetTexture("_MainTex");
            }
            if (texture != null)
                destination.SetTexture("_BaseMap", texture);

            Color tint = Color.white;
            if (source.HasProperty("_Tint"))
                tint = source.GetColor("_Tint");
            else if (source.HasProperty("_BaseColor"))
                tint = source.GetColor("_BaseColor");
            else if (source.HasProperty("_Color"))
                tint = source.GetColor("_Color");
            destination.SetColor("_Tint", tint);
        }

        static Vector3 GetBaseVisualScale(
            Mesh mesh,
            Vector3 dimensions,
            SolverRenderProfile renderProfile)
        {
            if (!renderProfile.fitMeshToDimensions)
                return renderProfile.visualScale;

            Vector3 target;
            switch (renderProfile.forwardAxis)
            {
                case SolverMeshForwardAxis.X:
                    target = new Vector3(
                        dimensions.y,
                        dimensions.x,
                        dimensions.z);
                    break;
                case SolverMeshForwardAxis.Z:
                    target = new Vector3(
                        dimensions.x,
                        dimensions.z,
                        dimensions.y);
                    break;
                default:
                    target = dimensions;
                    break;
            }

            Vector3 size = mesh.bounds.size;
            Vector3 fit = new Vector3(
                target.x /
                    Mathf.Max(0.000001f, size.x),
                target.y /
                    Mathf.Max(0.000001f, size.y),
                target.z /
                    Mathf.Max(0.000001f, size.z));
            return Vector3.Scale(
                fit,
                renderProfile.visualScale);
        }

        static float AxisComponent(
            Vector3 value,
            SolverMeshForwardAxis axis)
        {
            switch (axis)
            {
                case SolverMeshForwardAxis.X:
                    return value.x;
                case SolverMeshForwardAxis.Z:
                    return value.z;
                default:
                    return value.y;
            }
        }

        void DestroyRuntimeMaterial()
        {
            if (_runtimeMaterial == null)
                return;
            if (Application.isPlaying)
                Destroy(_runtimeMaterial);
            else
                DestroyImmediate(_runtimeMaterial);
            _runtimeMaterial = null;
            _activeRenderProfile = null;
        }

        void OnDestroy()
        {
            DestroyRuntimeMaterial();
        }

        void OnValidate()
        {
            drawBoundsSize =
                Mathf.Max(1f, drawBoundsSize);
        }
    }
}
