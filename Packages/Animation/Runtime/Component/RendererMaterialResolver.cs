using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Abstract ScriptableObject base for resolvers that update a <see cref="Renderer"/>'s
    /// material each frame. Concrete subclasses define the strategy
    /// (e.g. <see cref="TextureSheetResolver"/>, <see cref="TextureSequenceResolver"/>).
    /// </summary>
    /// <remarks>
    /// <para><b>State policy:</b> base uses <see cref="UnityEngine.Time.time"/> to compute the
    /// current frame — no internal state. Multiple renderers using the same SO instance
    /// animate in sync (often desired for grouped objects). If you need independent animation
    /// per renderer, duplicate the SO asset.</para>
    /// </remarks>
    public abstract class RendererMaterialResolver : ScriptableObject
    {
        [Tooltip("Material property name to manipulate. e.g. _BaseMap (URP Lit), _MainTex (Built-in).")]
        [SerializeField] protected string textureProperty = "_BaseMap";

        [Tooltip("Frames per second for time-driven frame index.")]
        [SerializeField, Min(0f)] protected float fps = 12f;

        protected int _propertyId;

        protected virtual void OnEnable() => CachePropertyId();
        protected virtual void OnValidate() => CachePropertyId();

        private void CachePropertyId()
        {
            if (!string.IsNullOrEmpty(textureProperty))
                _propertyId = Shader.PropertyToID(textureProperty);
        }

        /// <summary>
        /// Compute the current frame index from <see cref="Time.time"/> × <see cref="fps"/>,
        /// modulo <paramref name="frameCount"/>. All renderers using this SO see the same frame
        /// at the same Time.time, so they animate in sync.
        /// </summary>
        protected int CurrentFrame(int frameCount)
        {
            if (frameCount <= 0) return 0;
            return (int)(Time.time * fps) % frameCount;
        }

        /// <summary>Apply this resolver's logic to the renderer's material this frame.</summary>
        public abstract void Process(Renderer renderer);
    }
}
