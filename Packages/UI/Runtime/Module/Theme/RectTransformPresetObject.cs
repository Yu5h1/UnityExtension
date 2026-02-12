using UnityEngine;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.UI
{
    [Icon("d_Preset.Context")]
    public class RectTransformPresetObject : ParameterObject<RectTransformPreset> {}

	[System.Serializable]
	public class RectTransformPreset : ComponentPreset<RectTransform>
    {
        public Optional<Vector2> anchorMin;
        public Optional<Vector2> anchorMax;
        public Optional<Vector2> pivot;
        public Optional<Vector2> sizeDelta;
        public Optional<Vector2> anchoredPosition;

        public override bool ApplyTo(RectTransform rect)
        {
            if (anchorMin.TryGetValue(out var min)) rect.anchorMin = min;
            if (anchorMax.TryGetValue(out var max)) rect.anchorMax = max;
            if (pivot.TryGetValue(out var p)) rect.pivot = p;
            if (sizeDelta.TryGetValue(out var size)) rect.sizeDelta = size;
            if (anchoredPosition.TryGetValue(out var pos)) rect.anchoredPosition = pos;
            return true;
        }
    } 
}
