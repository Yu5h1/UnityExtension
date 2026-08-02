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
        [Tooltip("Rigid profiles are drawn with this material directly, so any URP or HDRP material works, including one taken off the shelf. Articulated profiles skin their mesh in a shader of their own and can only borrow this material's base map and tint.")]
        public Material sourceMaterial;

        [Header("Shader References")]
        [Tooltip("Articulated profiles only. Assign the package shader to prevent build stripping; the renderer falls back to Shader.Find when empty. Rigid profiles need no shader reference, because they are drawn with the Material above as-is.")]
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
