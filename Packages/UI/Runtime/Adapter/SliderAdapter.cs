using System.Globalization;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.UI;


namespace Yu5h1Lib.UI
{
    [AdapterRegistration(typeof(Slider), typeof(IValuePortAdapter<float>))]
    public class SliderAdapter : ValuePortAdapter<Slider, float>
    {
        [Preserve]
        public SliderAdapter(Slider component) : base(component) {}

        public override float value { get => c.value; set => c.value = value; }

        public override event UnityAction<float> ChangedCallback
        {
            add => c.onValueChanged.AddListener(value);
            remove => c.onValueChanged.RemoveListener(value);
        }
        public override string GetValue() => value.ToString(CultureInfo.InvariantCulture);
        public override void SetValue(string valueText)
        {
            if (float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ||
                float.TryParse(valueText, NumberStyles.Float, CultureInfo.CurrentCulture, out val))
                value = val;
        }

        public override void NotifyValueChanged() => c.onValueChanged?.Invoke(c.value);
    }

}