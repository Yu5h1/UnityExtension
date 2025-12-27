using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [OpsRegistration(typeof(Dropdown), typeof(IDropDownOps))]
    public class UI_DropDownOps : DropDownOps<Dropdown,Dropdown.OptionData>, IDropDownOps
    {
        [Preserve] public UI_DropDownOps(Dropdown d) : base(d) {}

        public override int current { get => c.value; set => c.value = value; }
        public override List<Dropdown.OptionData> options => c.options;
        public override Dropdown.OptionData CreateData(string text) => new(text);
        public override void InvokeChangedCallback() => c.onValueChanged.Invoke(c.value);
        public override event UnityAction<int> valueChanged
        {
            add => c.onValueChanged.AddListener(value);
            remove => c.onValueChanged.RemoveListener(value);
        }

        public override string GetText(Dropdown.OptionData data) => data.text;
        public override void SetText(Dropdown.OptionData data, string value) => data.text = value;
        protected override void Refresh() => c.RefreshShownValue();


    }
}
