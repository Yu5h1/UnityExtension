using UnityEngine;

namespace Yu5h1Lib.Theming
{
    /// <summary>
    /// Sprite 目標群組 - 套用到 SpriteRenderer (非 UI)
    /// </summary>
    public class SpriteRendererBinding: Theme.BindingObject<SpriteRenderer,Sprite>
    {
        protected override void SetValue(SpriteRenderer c, Sprite s) => c.sprite = s;
    } 
}