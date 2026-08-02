using System.Collections.Generic;
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
        const string ArticulatedShaderName =
            "Yu5h1/UnifiedSolver/ArticulatedMesh";

        // Graphics.RenderMeshInstanced draws at most this many per call.
        const int InstancesPerBatch = 1023;

        [Min(1f)]
        public float drawBoundsSize = 1000f;

        SolverParticleEmitter _emitter;
        MaterialPropertyBlock _properties;
        Material _runtimeMaterial;
        SolverRenderProfile _activeRenderProfile;
        bool _reportedMissingSetup;

        // Rigid path. One mesh per template, built once from the same vertices
        // the emitter used as rest particles.
        Mesh[] _templateMeshes;
        SolverShapeSource _templateSource;
        Vector3 _templateDimensions;
        Matrix4x4[] _matrices =
            new Matrix4x4[InstancesPerBatch];
        Vector3[] _templateScratch = new Vector3[12];

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

            if (_emitter.InstanceCount == 0 ||
                _emitter.Solver == null)
            {
                return;
            }

            // A rigid body is a mesh and a matrix, and the solver already hands
            // out that matrix on the CPU every frame through
            // TryGetRigidBodyMeshPose. So rigid instances are drawn with
            // Graphics.RenderMeshInstanced and an ordinary material: no custom
            // shader, which means any URP or HDRP material works unmodified,
            // along with everything the pipeline gives a normal renderer.
            //
            // Only articulated bodies still need a shader of their own. Their
            // vertices are skinned from particle positions every frame and
            // cannot be expressed as a single transform.
            if (profile.MeshMode == SolverMeshMode.Rigid)
                DrawRigid(profile, renderProfile);
            else
                DrawArticulated(profile, renderProfile);
        }

        void DrawRigid(
            SolverParticleProfile profile,
            SolverRenderProfile renderProfile)
        {
            Material material = renderProfile.sourceMaterial;
            if (material == null)
            {
                ReportOnce(
                    "its Render Profile has no Material. Rigid " +
                    "instances are drawn with it directly, so any " +
                    "URP or HDRP material will do");
                return;
            }

            // A material whose shader was deleted still loads: Unity swaps in
            // the internal error shader and the result is magenta, or with
            // instancing, nothing at all. Worth naming, because the material
            // looks correctly assigned in the inspector and the fault is one
            // level below it.
            if (material.shader == null ||
                material.shader.name ==
                    "Hidden/InternalErrorShader")
            {
                ReportOnce(
                    $"material '{material.name}' has no working " +
                    "shader; the one it was authored against is " +
                    "missing. Pick a shader on the material, for " +
                    "example Universal Render Pipeline/Lit");
                return;
            }

            // RenderMeshInstanced refuses a material without GPU instancing.
            // Setting the flag on the assigned asset would dirty something
            // shared, so the runtime copy carries it instead.
            if (_runtimeMaterial == null ||
                _activeRenderProfile != renderProfile)
            {
                DestroyRuntimeMaterial();
                _runtimeMaterial = new Material(material)
                {
                    name = $"{material.name} (Runtime)",
                    enableInstancing = true
                };
                _activeRenderProfile = renderProfile;
            }

            var parameters =
                new RenderParams(_runtimeMaterial)
                {
                    worldBounds = new Bounds(
                        transform.position,
                        Vector3.one * drawBoundsSize),
                    shadowCastingMode =
                        renderProfile.castShadows
                            ? ShadowCastingMode.On
                            : ShadowCastingMode.Off,
                    receiveShadows =
                        renderProfile.receiveShadows,
                    layer = gameObject.layer
                };

            if (profile.UsesHullRendering)
            {
                DrawHullTemplates(profile, parameters);
                return;
            }

            if (renderProfile.mesh == null)
            {
                ReportOnce(
                    $"profile '{profile.name}' is rigid but " +
                    "has neither a Mesh on its Render Profile " +
                    "nor a Shape Source to build particle hulls " +
                    "from, so there is no geometry to draw");
                return;
            }

            DrawAuthoredMesh(renderProfile, parameters);
        }

        // One instanced call per template, because RenderMeshInstanced takes a
        // single mesh and the templates are genuinely different shapes. This is
        // why the shape source hands out a fixed library rather than a unique
        // shape per instance: a shape nothing else shares cannot be batched.
        void DrawHullTemplates(
            SolverParticleProfile profile,
            RenderParams parameters)
        {
            if (!EnsureTemplateMeshes(profile))
                return;

            for (int template = 0;
                 template < _templateMeshes.Length;
                 template++)
            {
                Mesh mesh = _templateMeshes[template];
                if (mesh == null)
                    continue;

                IReadOnlyList<int> bodies =
                    _emitter.TemplateBodies(template);
                if (bodies == null || bodies.Count == 0)
                    continue;

                DrawBodies(mesh, bodies, parameters);
            }
        }

        // Every rigid instance shares the authored mesh, so one pass covers all
        // of them. visualScale is applied through the matrix here, which is safe
        // because an authored mesh carries no baked particle-radius inflation
        // for a scale to stretch.
        void DrawAuthoredMesh(
            SolverRenderProfile renderProfile,
            RenderParams parameters)
        {
            Mesh mesh = renderProfile.mesh;
            if (mesh == null)
                return;

            int filled = 0;
            for (int i = 0;
                 i < _emitter.InstanceCount;
                 i++)
            {
                SolverParticleInstance instance =
                    _emitter.Instances[i];
                if (instance.rigidBodyCount <= 0)
                    continue;
                if (!TryGetMatrix(
                        instance.rigidBodyOffset,
                        Vector3.Scale(
                            renderProfile.visualScale,
                            instance.scale),
                        out _matrices[filled]))
                {
                    continue;
                }

                filled++;
                if (filled == InstancesPerBatch)
                {
                    Flush(parameters, mesh, filled);
                    filled = 0;
                }
            }

            Flush(parameters, mesh, filled);
        }

        void DrawBodies(
            Mesh mesh,
            IReadOnlyList<int> bodies,
            RenderParams parameters)
        {
            int filled = 0;
            for (int i = 0; i < bodies.Count; i++)
            {
                // Template meshes are already the right size, and their
                // particle-radius inflation was baked in local space, so a
                // scale here would stretch that inflation with it.
                if (!TryGetMatrix(
                        bodies[i],
                        Vector3.one,
                        out _matrices[filled]))
                {
                    continue;
                }

                filled++;
                if (filled == InstancesPerBatch)
                {
                    Flush(parameters, mesh, filled);
                    filled = 0;
                }
            }

            Flush(parameters, mesh, filled);
        }

        void Flush(
            RenderParams parameters,
            Mesh mesh,
            int count)
        {
            if (count <= 0)
                return;

            Graphics.RenderMeshInstanced(
                parameters,
                mesh,
                0,
                _matrices,
                count);
        }

        // The solver composes the shape-matched rotation with the spawn
        // transform, so this maps unrotated template space straight to world,
        // which is the same space the rest particles were built in.
        bool TryGetMatrix(
            int rigidBodyId,
            Vector3 scale,
            out Matrix4x4 matrix)
        {
            if (!_emitter.Solver.TryGetRigidBodyMeshPose(
                    rigidBodyId,
                    out Vector3 position,
                    out Quaternion rotation))
            {
                matrix = Matrix4x4.identity;
                return false;
            }

            matrix = Matrix4x4.TRS(
                position,
                rotation,
                scale);
            return true;
        }

        // Meshes are built from a second BuildTemplate call with the same
        // arguments the emitter used, so the drawn surface and the collision
        // shape come from one source and cannot drift apart.
        bool EnsureTemplateMeshes(
            SolverParticleProfile profile)
        {
            SolverShapeSource source = profile.shapeSource;
            if (source == null)
                return false;

            if (_templateMeshes != null &&
                _templateSource == source &&
                _templateDimensions ==
                    profile.baseDimensions &&
                _templateMeshes.Length ==
                    source.TemplateCount)
            {
                return true;
            }

            ReleaseTemplateMeshes();
            int count = Mathf.Max(1, source.TemplateCount);
            if (_templateScratch.Length <
                source.MaximumParticles)
            {
                _templateScratch =
                    new Vector3[source.MaximumParticles];
            }

            _templateMeshes = new Mesh[count];
            for (int i = 0; i < count; i++)
            {
                SolverParticleTopology topology =
                    source.BuildTemplate(
                        i,
                        profile.baseDimensions,
                        _templateScratch,
                        out int vertexCount);
                _templateMeshes[i] = SolverHullMesh.Build(
                    topology,
                    _templateScratch,
                    vertexCount,
                    _emitter.Solver.particleRadius);
            }

            _templateSource = source;
            _templateDimensions = profile.baseDimensions;
            return true;
        }

        void ReleaseTemplateMeshes()
        {
            if (_templateMeshes == null)
                return;

            for (int i = 0; i < _templateMeshes.Length; i++)
            {
                if (_templateMeshes[i] == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(_templateMeshes[i]);
                else
                    DestroyImmediate(_templateMeshes[i]);
            }
            _templateMeshes = null;
            _templateSource = null;
        }

        void DrawArticulated(
            SolverParticleProfile profile,
            SolverRenderProfile renderProfile)
        {
            if (renderProfile.mesh == null)
            {
                ReportOnce(
                    $"profile '{profile.name}' is articulated " +
                    "and its Render Profile has no Mesh. A body " +
                    "that deforms every frame is skinned from a " +
                    "supplied mesh; there is nothing to derive " +
                    "one from");
                return;
            }

            if (_runtimeMaterial == null ||
                _activeRenderProfile != renderProfile)
            {
                CreateArticulatedMaterial(renderProfile);
            }

            if (_runtimeMaterial == null ||
                _emitter.InstanceBuffer == null ||
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

            Graphics.DrawMeshInstancedProcedural(
                mesh,
                0,
                _runtimeMaterial,
                new Bounds(
                    transform.position,
                    Vector3.one * drawBoundsSize),
                _emitter.InstanceCount,
                _properties,
                renderProfile.castShadows
                    ? ShadowCastingMode.On
                    : ShadowCastingMode.Off,
                renderProfile.receiveShadows,
                gameObject.layer);
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
                profile != null && renderProfile != null;
            if (!valid && !_reportedMissingSetup)
            {
                ReportOnce(
                    profile == null
                        ? "no Particle Profile is assigned"
                        : $"profile '{profile.name}' has no " +
                          "Render Profile");
            }
            if (valid)
                _reportedMissingSetup = false;
            return valid;
        }

        // Once per fault, and it names the missing piece. The earlier message
        // asked for all three whichever one was absent, which is no help when
        // two are already assigned.
        void ReportOnce(string reason)
        {
            if (_reportedMissingSetup)
                return;

            _reportedMissingSetup = true;
            Debug.LogWarning(
                $"SolverMeshRenderer on '{name}': nothing " +
                $"can be drawn because {reason}.",
                this);
        }

        void CreateArticulatedMaterial(
            SolverRenderProfile renderProfile)
        {
            DestroyRuntimeMaterial();
            Shader shader =
                renderProfile.articulatedShader != null
                    ? renderProfile.articulatedShader
                    : Shader.Find(ArticulatedShaderName);
            if (shader == null)
            {
                Debug.LogError(
                    "SolverMeshRenderer: the articulated " +
                    "shader was not found. Assign it on the " +
                    "Render Profile.",
                    this);
                return;
            }

            _runtimeMaterial = new Material(shader)
            {
                name = $"{shader.name} (Runtime)"
            };
            _activeRenderProfile = renderProfile;
            CopyMaterialProperties(
                renderProfile.sourceMaterial,
                _runtimeMaterial);
        }

        // Articulated only. The rigid path uses the assigned material as-is, so
        // it needs no property forwarding at all.
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
            ReleaseTemplateMeshes();
        }

        void OnValidate()
        {
            drawBoundsSize =
                Mathf.Max(1f, drawBoundsSize);
        }
    }
}
