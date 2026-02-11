using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
	public class ImageHelper : UIControl<Image>
	{
        [SerializeField, Range(0, 1)]
        private float _alphaThreshold = 0.1f;

        protected override void OnInitializing()
        {
            base.OnInitializing();
            ui.alphaHitTestMinimumThreshold = _alphaThreshold;
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (ui != null)
            {
                ui.alphaHitTestMinimumThreshold = _alphaThreshold;
            }
        } 
#endif
    } 
}
