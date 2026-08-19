using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Scrolls a texture offset continuously, driven by <see cref="Time.time"/>. Produces flowing surfaces
    /// such as water jets, waterfalls, conveyor belts and energy beams.
    /// <para>Continuous rather than frame-quantized, which is why this is the one driver holding no
    /// <see cref="FrameStepResolver"/>. Use <see cref="TextureSheetDriver"/> to step through discrete cells.</para>
    /// <para>Tiling is left alone — set it on the material. The offset is wrapped into <c>[0,1)</c> every
    /// frame so precision does not degrade over a long session.</para>
    /// </summary>
    public class TextureScrollDriver : MaterialDriver
    {
        [Tooltip("UV units scrolled per second. X scrolls along U, Y along V. Negative reverses direction.")]
        [SerializeField] private Vector2 speed = new Vector2(1f, 0f);

        public override void Drive(IReadOnlyList<Material> materials)
        {
            if (materials == null)
                return;

            var scrolled = speed * Time.time;
            var offset = new Vector2(Mathf.Repeat(scrolled.x, 1f), Mathf.Repeat(scrolled.y, 1f));

            for (int i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material != null)
                    material.SetTextureOffset(propertyId, offset);
            }
        }
    }
}
