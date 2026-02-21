using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.Theming
{
    /// <summary>
    /// Sprite 目標群組 - 套用到 Image
    /// </summary>
    public class ImageSpriteBinding : Theme.BindingObject<Image,Sprite>
    {
        protected override void SetValue(Image c, Sprite value) =>  c.sprite = value;
    }
}
