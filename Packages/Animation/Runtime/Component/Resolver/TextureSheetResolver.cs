using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Animates a sprite sheet by manipulating the material's UV scale + offset.
    /// One texture is sliced into a <c>columns × rows</c> grid; each frame uses one cell.
    /// <para>Equivalent to the legacy <see cref="MaterialTextureSheetController"/> behavior, now as a swappable SO.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "Yu5h1Lib/Renderer Material/Texture Sheet", fileName = "TextureSheet")]
    public class TextureSheetResolver : RendererMaterialResolver
    {
        [Tooltip("Number of columns in the sprite sheet grid.")]
        [SerializeField, Min(1)] private int columns = 4;

        [Tooltip("Number of rows in the sprite sheet grid.")]
        [SerializeField, Min(1)] private int rows = 4;

        public override void Process(Renderer renderer)
        {
            int frameCount = columns * rows;
            int frame = CurrentFrame(frameCount);

            float scaleX = 1f / columns;
            float scaleY = 1f / rows;
            int col = frame % columns;
            int row = frame / columns;

            var mat = renderer.material;
            mat.SetTextureScale(_propertyId, new Vector2(scaleX, scaleY));
            // 1f - (row + 1) * scaleY — flip Y so frame 0 is top-left of the sheet.
            mat.SetTextureOffset(_propertyId, new Vector2(col * scaleX, 1f - (row + 1) * scaleY));
        }
    }
}
