using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1.UnifiedSolver
{
    // No RequireComponent back to the emitter.
    //
    // The emitter owns this component's whole lifecycle, and a
    // requirement pointing back at it would block removing the emitter
    // with a dialog naming a component the user cannot see, because this
    // one is hidden. Standalone use is handled by resolving the emitter
    // defensively instead.
    public sealed class SolverMeshRenderer : MonoBehaviour
    {
        const string RigidShaderName =
            "Yu5h1/UnifiedSolver/RigidMesh";
        const string ArticulatedShaderName =
            "Yu5h1/UnifiedSolver/ArticulatedMesh";

        const string HullKeyword =
            "SOLVER_HULL_FROM_PARTICLES";

        static readonly SolverParticleTopology[]
            HullVariants =
            {
                SolverParticleTopology.RigidCluster4,
                SolverParticleTopology.RigidCluster6,
                SolverParticleTopology.RigidCluster8
            };

        [Min(1f)]
        public float drawBoundsSize = 1000f;

        SolverParticleEmitter _emitter;
        Material _runtimeMaterial;
        MaterialPropertyBlock _properties;
        SolverRenderProfile _activeRenderProfile;
        bool _activeHullMode;
        readonly Mesh[] _hullMeshes = new Mesh[3];
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

            // Rebuild on the derived mode too, not just on profile identity.
            // Assigning or clearing the Mesh switches between hull and authored
            // drawing without changing which render profile is referenced, and
            // the shader keyword is baked into the material.
            bool hull = profile.UsesHullRendering;
            if (_runtimeMaterial == null ||
                _activeRenderProfile != renderProfile ||
                _activeHullMode != hull)
            {
                CreateRuntimeMaterial(profile, renderProfile);
            }

            if (_runtimeMaterial == null ||
                _emitter.InstanceCount == 0 ||
                _emitter.InstanceBuffer == null ||
                _emitter.Solver == null ||
                _emitter.Solver.ParticleBuffer == null)
            {
                return;
            }

            if (profile.UsesHullRendering)
            {
                DrawHull(profile, renderProfile);
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
            if (!TryBindRigidBuffers(profile))
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

        // One draw per rigid cluster variant.
        //
        // DrawMeshInstancedProcedural takes a single mesh, and the three
        // variants have three different face lists, so they cannot share a call.
        // Each draw covers only the instances of its own variant and the shader
        // maps the batch-local id back through the emitter's variant index
        // buffer. This is also the batch-to-instance mapping baked fracture
        // fragments will need, where the split is per fragment mesh rather than
        // per variant.
        void DrawHull(
            SolverParticleProfile profile,
            SolverRenderProfile renderProfile)
        {
            if (!SolverManagerAccess
                    .TryGetRigidRestOffsets(
                        _emitter.Solver,
                        out ComputeBuffer restOffsets))
            {
                return;
            }

            Bounds drawBounds = new Bounds(
                transform.position,
                Vector3.one * drawBoundsSize);
            ShadowCastingMode shadows =
                renderProfile.castShadows
                    ? ShadowCastingMode.On
                    : ShadowCastingMode.Off;

            for (int i = 0;
                 i < HullVariants.Length;
                 i++)
            {
                SolverParticleTopology variant =
                    HullVariants[i];
                if (!_emitter.TryGetVariantBatch(
                        variant,
                        out ComputeBuffer variantIndices,
                        out int variantOffset,
                        out int variantCount))
                {
                    continue;
                }

                Mesh mesh = ResolveHullMesh(i, variant);
                if (mesh == null)
                    continue;

                _properties.Clear();
                _properties.SetBuffer(
                    "_Particles",
                    _emitter.Solver.ParticleBuffer);
                _properties.SetBuffer(
                    "_Instances",
                    _emitter.InstanceBuffer);
                if (!TryBindRigidBuffers(profile))
                    return;

                _properties.SetBuffer(
                    "_RigidRestOffsets",
                    restOffsets);
                _properties.SetBuffer(
                    "_VariantInstances",
                    variantIndices);
                _properties.SetInt(
                    "_VariantOffset",
                    variantOffset);
                _properties.SetFloat(
                    "_ParticleRadius",
                    _emitter.Solver.particleRadius);

                Graphics.DrawMeshInstancedProcedural(
                    mesh,
                    0,
                    _runtimeMaterial,
                    drawBounds,
                    variantCount,
                    _properties,
                    shadows,
                    renderProfile.receiveShadows,
                    gameObject.layer);
            }
        }

        Mesh ResolveHullMesh(
            int slot,
            SolverParticleTopology variant)
        {
            if (_hullMeshes[slot] == null)
            {
                _hullMeshes[slot] =
                    SolverHullMesh.Build(variant);
            }
            return _hullMeshes[slot];
        }

        bool TryBindRigidBuffers(
            SolverParticleProfile profile)
        {
            if (profile.MeshMode !=
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

            // Hull rendering builds its own meshes from the variant face lists,
            // so a rigid profile needs no Mesh at all. Only an articulated one
            // does, because there is nothing to derive its surface from.
            bool valid =
                profile != null &&
                renderProfile != null &&
                (renderProfile.mesh != null ||
                 profile.UsesHullRendering);
            if (!valid && !_reportedMissingSetup)
            {
                // Name the missing piece. The previous message asked for all
                // three whichever one was absent, which is no help when two are
                // already assigned.
                string missing =
                    profile == null
                        ? "no Particle Profile is assigned"
                        : renderProfile == null
                            ? $"profile '{profile.name}' has no " +
                              "Render Profile"
                            : $"profile '{profile.name}' is " +
                              "articulated, so its Render Profile " +
                              "needs a Mesh; only rigid profiles " +
                              "can be drawn from their particles";
                Debug.LogWarning(
                    $"SolverMeshRenderer on '{name}': " +
                    $"nothing can be drawn because {missing}.",
                    this);
                _reportedMissingSetup = true;
            }
            if (valid)
                _reportedMissingSetup = false;
            return valid;
        }

        void CreateRuntimeMaterial(
            SolverParticleProfile profile,
            SolverRenderProfile renderProfile)
        {
            DestroyRuntimeMaterial();
            bool rigid =
                profile.MeshMode == SolverMeshMode.Rigid;
            Shader shader =
                rigid
                    ? renderProfile.rigidShader
                    : renderProfile.articulatedShader;
            if (shader == null)
            {
                shader = Shader.Find(
                    rigid
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
            if (profile.UsesHullRendering)
                _runtimeMaterial.EnableKeyword(HullKeyword);
            else
                _runtimeMaterial.DisableKeyword(HullKeyword);
            _activeRenderProfile = renderProfile;
            _activeHullMode = profile.UsesHullRendering;
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
            for (int i = 0; i < _hullMeshes.Length; i++)
            {
                if (_hullMeshes[i] == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(_hullMeshes[i]);
                else
                    DestroyImmediate(_hullMeshes[i]);
                _hullMeshes[i] = null;
            }
        }

        void OnValidate()
        {
            drawBoundsSize =
                Mathf.Max(1f, drawBoundsSize);
        }
    }
}
