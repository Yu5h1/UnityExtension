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
        public override string GetValue() => value.ToString();
        public override void SetValue(string valueText)
        { 
            if (float.TryParse(valueText, out float val))
                value = val;
        }
    }

}