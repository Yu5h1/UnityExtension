using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Slider)),AddonFor(typeof(Slider))]
    public class SliderAddon : UIControl<Slider, float>
    {
        public override float value { get => ui.value; set => ui.value = value; }

        public override void AddListener(UnityAction<float> method)
            => ui.onValueChanged.AddListener(method);

        public override void RemoveListener(UnityAction<float> method) 
            => ui.onValueChanged.RemoveListener(method);

        public override bool TryParse(string value, out float result) 
            => float.TryParse(value, out result);
    }
}