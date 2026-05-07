using System;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.UI;
using Yu5h1Lib.Common;
using Yu5h1Lib.MVVM;

namespace Yu5h1Lib.UI
{
    [AdapterRegistration(typeof(Toggle), typeof(IValuePortAdapter<bool>))]
    public class ToggleAdapter : ValuePortAdapter<Toggle,bool>
    {
        [Preserve]
        public ToggleAdapter(Toggle component) : base(component) {}

        public override bool value { get => c.isOn ; set => c.isOn = value; }

        public override event UnityAction<bool> ChangedCallback
        {
            add => c.onValueChanged.AddListener(value);
            remove => c.onValueChanged.RemoveListener(value);
        }

        public override string GetValue() => c?.isOn == true ? "true" : "false";
        public override void SetValue(string value) => c.isOn = value.Equals("true",StringComparison.OrdinalIgnoreCase) || value == "1";
    }
}
