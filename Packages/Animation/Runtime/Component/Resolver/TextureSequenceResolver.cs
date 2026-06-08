using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Animates by swapping the material's full texture between multiple <see cref="Texture"/> assets.
    /// Each frame uses a different texture — unlike <see cref="TextureSheetResolver"/> which slices one sheet.
    /// </summary>
    [CreateAssetMenu(menuName = "Yu5h1Lib/Renderer Material/Texture Sequence", fileName = "TextureSequence")]
    public class TextureSequenceResolver : RendererMaterialResolver
    {
        [Tooltip("Textures to cycle through. Order = animation sequence; loops back to 0 after last.")]
        [SerializeField] private Texture[] textures;

        public override void Process(Renderer renderer)
        {
            if (textures == null || textures.Length == 0) return;

            int frame = CurrentFrame(textures.Length);
            var tex = textures[frame];
            if (tex == null) return;

            renderer.material.SetTexture(_propertyId, tex);
        }
    }
}
