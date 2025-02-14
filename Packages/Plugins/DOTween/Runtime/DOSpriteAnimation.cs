using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib
{
    public class DOSpriteAnimation : DOCounter<Component>
    {
        public Sprite[] sprites;

        public override Component OverrideGetComponent()
        {
            if (transform is RectTransform m)
                return m.GetComponent<Image>();
            return GetComponent<SpriteRenderer>();
        }

        protected override void OnIndexChanged()
        {
            if (sprites.IsEmpty() || !sprites.IsValid(index))
                return;
            SetSprite(index);
        }
        public void SetSprite(int frame)
        {
            switch (component)
            {
                case SpriteRenderer renderer:
                    renderer.sprite = sprites[frame];
                    break;
                case Image image:
                    image.sprite = sprites[frame];
                    break;
            }
        }
    }
}
