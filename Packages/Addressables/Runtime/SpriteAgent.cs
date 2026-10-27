using UnityEngine;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// The scene's stand-in for an image that exists only after loading, such as a character's portrait:
    /// <see cref="ContentAgent{TAsset,TResult}.Prepare"/> loads the Sprite at a key and the request raises Loaded with it.
    /// Wire Loaded to what shows it (UI Image.sprite, SpriteRenderer.sprite); the package does not reference UI.
    /// Keys, cancellation and release follow <see cref="ContentAgent{TAsset,TResult}"/>: a new sprite replaces the old one
    /// only after Loaded handed it out, and <c>Release</c> gives the sprite back, so clear what shows it first.
    /// The key may address a texture imported as a Sprite, or a sprite inside an atlas texture as <c>address[name]</c>.
    /// </summary>
    public class SpriteAgent : ContentAgent<Sprite, Sprite>
    {
        /// <summary>The sprite currently shown; null before the first successful load or after <c>Release</c>.</summary>
        public Sprite sprite => current;

        protected override ContentLoad<Sprite> CreateRequest() => ScriptableObject.CreateInstance<SpriteLoad>();

        protected override Sprite Produce(Sprite asset, string key) => asset;
    }
}
