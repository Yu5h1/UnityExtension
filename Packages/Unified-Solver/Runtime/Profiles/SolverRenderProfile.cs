using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // How an instance is drawn. Only genuine drawing choices belong here.
    //
    // Which drawing algorithm applies is not one of them: it follows entirely
    // from the particle profile's topology and shape source, and is derived
    // rather than restated. Two fields that did restate it, `meshMode` and
    // `hullFromParticles`, are gone. Both had to agree with the physics for
    // anything to appear, neither had a default that agreed with a rigid
    // fragment setup, and disagreeing produced no error and no geometry, which
    // is the worst possible way for a mistake to present.
    public sealed class SolverRenderProfile : ScriptableObject
    {
        [Tooltip("Leave empty on a rigid profile to draw each instance as the convex hull of its own particles: the drawn surface is then the same points the solver collides with, so a procedurally varied fragment needs no mesh asset and physics and visuals cannot drift apart. Assign a mesh to draw that instead. Articulated profiles always need one.")]
        public Mesh mesh;
        public Material sourceMaterial;

        [Header("Shader References")]
        [Tooltip("Assign the package shader to prevent build stripping. The renderer falls back to Shader.Find when empty.")]
        public Shader rigidShader;
        public Shader articulatedShader;

        [Header("Mesh Mapping")]
        public SolverMeshForwardAxis forwardAxis =
            SolverMeshForwardAxis.Y;
        public bool fitMeshToDimensions = true;
        public Vector3 visualScale = Vector3.one;

        [Header("Shadows")]
        public bool castShadows = true;
        public bool receiveShadows = true;

        void OnValidate()
        {
            visualScale = new Vector3(
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(visualScale.x)),
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(visualScale.y)),
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(visualScale.z)));
        }
    }
}
