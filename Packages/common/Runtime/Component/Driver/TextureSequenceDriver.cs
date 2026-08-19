using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Animates by swapping the whole texture between several <see cref="Texture"/> assets, one per frame.
    /// Every material receives the same texture.
    /// <para>Use <see cref="TextureSheetDriver"/> when the frames are cells of a single sheet instead.</para>
    /// </summary>
    public class TextureSequenceDriver : MaterialDriver
    {
        [Tooltip("Textures to cycle through. Order is the animation sequence; loops back to the first after the last.")]
        [SerializeField] private Texture[] textures;

        [SerializeField] private FrameStepResolver frameStep = new FrameStepResolver();

        public override void Drive(IReadOnlyList<Material> materials)
        {
            if (materials == null || textures == null || textures.Length == 0)
                return;

            var texture = textures[frameStep.Resolve(textures.Length)];
            if (texture == null)
                return;

            for (int i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                if (material != null)
                    material.SetTexture(propertyId, texture);
            }
        }
    }
}
