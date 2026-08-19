using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Animates a sprite sheet by writing UV scale and offset. One texture is treated as a
    /// <c>columns × rows</c> grid and each frame selects one cell; every material receives the same cell.
    /// <para>The offset counts rows from the bottom so that frame 0 is the top-left cell, which is how
    /// sheets are read.</para>
    /// <para>Use <see cref="TextureSequenceDriver"/> when the frames are separate textures rather than
    /// cells of one, and <see cref="TextureScrollDriver"/> when the motion should be continuous.</para>
    /// </summary>
    public class TextureSheetDriver : MaterialDriver
    {
        [Tooltip("Number of columns in the sprite sheet grid.")]
        [SerializeField, Min(1)] private int columns = 5;

        [Tooltip("Number of rows in the sprite sheet grid.")]
        [SerializeField, Min(1)] private int rows = 5;

        [SerializeField] private FrameStepResolver frameStep = new FrameStepResolver();

        public override void Drive(IReadOnlyList<Material> materials)
        {
            if (materials == null)
                return;

            int frame = frameStep.Resolve(columns * rows);
            var scale = new Vector2(1f / columns, 1f / rows);
            var offset = new Vector2(frame % columns * scale.x, 1f - (frame / columns + 1) * scale.y);

            for (int i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material == null)
                    continue;
                material.SetTextureScale(propertyId, scale);
                material.SetTextureOffset(propertyId, offset);
            }
        }
    }
}
