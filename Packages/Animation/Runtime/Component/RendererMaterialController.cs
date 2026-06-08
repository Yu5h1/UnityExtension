using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Drives a <see cref="Renderer"/>'s material every frame via a swappable
    /// <see cref="RendererMaterialResolver"/> SO. Behaviors (sprite-sheet UV, texture swap,
    /// future variants) are added by writing new resolver SOs — this component never changes.
    /// </summary>
    /// <remarks>
    /// Replaces <see cref="MaterialTextureSheetController"/>. Migrate by:
    /// <list type="number">
    ///   <item>Add this component to the same GameObject.</item>
    ///   <item>Create a <see cref="TextureSheetResolver"/> SO with the same columns/rows/fps.</item>
    ///   <item>Drop SO into <see cref="resolver"/> field.</item>
    ///   <item>Remove old <see cref="MaterialTextureSheetController"/>.</item>
    /// </list>
    /// </remarks>
    [RequireComponent(typeof(Renderer))]
    public class RendererMaterialController : ComponentController<Renderer>
    {
        [Tooltip("Resolver SO that defines how to update the material each frame. Swap this SO to change behavior without touching this component.")]
        [SerializeField,Inline] private RendererMaterialResolver resolver;


        private void Update()
        {
            if (resolver == null || component == null) return;
            resolver.Process(component);
        }

        /// <summary>Swap the active resolver at runtime.</summary>
        public void SetResolver(RendererMaterialResolver newResolver) => resolver = newResolver;
    }
}
