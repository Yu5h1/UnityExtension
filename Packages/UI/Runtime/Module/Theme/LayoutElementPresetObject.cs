using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [Icon("d_Preset.Context")]
    public class LayoutElementPresetObject : ParameterObject<LayoutElementPreset> {} 

	[System.Serializable]
	public class LayoutElementPreset : ComponentPreset<LayoutElement>
	{
        public Optional<float> minWidth;
        public Optional<float> minHeight;
        public Optional<float> preferredWidth;
        public Optional<float> preferredHeight;
        public Optional<float> flexibleWidth;
        public Optional<float> flexibleHeight;

        public override bool ApplyTo(LayoutElement layout)
        {
            if (minWidth.TryGetValue(out float minWidthValue))
                layout.minWidth = minWidthValue;
            if (minHeight.TryGetValue(out float minHeighValue))
                layout.minHeight = minHeighValue;
            if (preferredWidth.TryGetValue(out float maxWidthValue))
                layout.preferredWidth = maxWidthValue;
            if (preferredHeight.TryGetValue(out float minHeightValue))
                layout.preferredHeight = minHeightValue;
            if (flexibleWidth.TryGetValue(out float flexibleWidthValue))
                layout.flexibleWidth = flexibleWidthValue;
            if (flexibleHeight.TryGetValue(out float flexibleHeightValue))
                layout.flexibleHeight = flexibleHeightValue;

            return true;
        }
    }
}
