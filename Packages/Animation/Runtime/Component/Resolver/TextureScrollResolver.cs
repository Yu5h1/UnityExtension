using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Scrolls a material's texture offset continuously, driven by <see cref="Time.time"/>.
    /// Produces flowing surfaces such as water jets, waterfalls, conveyor belts, and energy beams.
    /// </summary>
    /// <remarks>
    /// <para>Continuous rather than frame-quantized, so the inherited <c>fps</c> field has no effect here.
    /// Use <see cref="TextureSheetResolver"/> when the animation should step through discrete cells.</para>
    /// <para>Tiling is left untouched — set it on the material itself. The offset is wrapped into
    /// <c>[0,1)</c> every frame so precision does not degrade during a long session.</para>
    /// </remarks>
    public class TextureScrollResolver : RendererMaterialResolver
    {
        [Tooltip("UV units scrolled per second. X scrolls along U, Y along V. Negative reverses direction.")]
        [SerializeField] private Vector2 speed = new Vector2(1f, 0f);

        public override void Process(Renderer renderer)
        {
            var offset = speed * Time.time;
            renderer.material.SetTextureOffset(
                _propertyId,
                new Vector2(Mathf.Repeat(offset.x, 1f), Mathf.Repeat(offset.y, 1f)));
        }
    }
}
