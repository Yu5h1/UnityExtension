using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
	public class ImageAddon : UI_GrahpicAddon<Image>
	{
        [SerializeField, Range(0, 1)]
        private float _alphaHitTestMinimumThreshold = 0.1f;
        public float alphaHitTestMinimumThreshold
        {
            get => _alphaHitTestMinimumThreshold;
            set
            {       
                if (_alphaHitTestMinimumThreshold == value)
                    return;
                _alphaHitTestMinimumThreshold = value;
                SetAlphaHitTestMinimumThreshold(value);
            }
        }
        
        protected override void OnInitializing()
        {
            base.OnInitializing();
            SetAlphaHitTestMinimumThreshold(alphaHitTestMinimumThreshold);
        }

        public void SetAlphaHitTestMinimumThreshold(float threshold)
        {
            if (ui == null)
                return;
            if (ui.sprite.texture.isReadable)
                ui.alphaHitTestMinimumThreshold = _alphaHitTestMinimumThreshold;
            //else
            //    Debug.LogWarning($"Sprite texture is not readable. Cannot set alphaHitTestMinimumThreshold for {ui.name}.");
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            SetAlphaHitTestMinimumThreshold(alphaHitTestMinimumThreshold);
        } 
#endif
    } 
}
