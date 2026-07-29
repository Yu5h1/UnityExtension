using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    [CreateAssetMenu(
        fileName = "SolverRenderProfile",
        menuName = "Yu5h1/Unified Solver/Render Profile")]
    public sealed class SolverRenderProfile : ScriptableObject
    {
        public SolverMeshMode meshMode =
            SolverMeshMode.Articulated;
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
