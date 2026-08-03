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
        // Empty on a rigid profile draws the convex hull of the instance's own
        // particles, so the drawn surface is the points the solver collides
        // with and a varied fragment needs no mesh asset.
        [Tooltip("Empty on a rigid profile draws its particle hull. Articulated needs one.")]
        public Mesh mesh;
        [Tooltip("Rigid uses this directly, so any URP or HDRP material works.")]
        public Material sourceMaterial;

        [Tooltip("Articulated only. Assign to prevent build stripping.")]
        public Shader articulatedShader;

        [Header("Mesh Mapping")]
        public SolverMeshForwardAxis forwardAxis =
            SolverMeshForwardAxis.Y;
        public bool fitMeshToDimensions = true;
        public Vector3 visualScale = Vector3.one;

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
