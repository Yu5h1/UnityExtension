using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Linq;
using UnityEngine.Events;

namespace Yu5h1Lib.UI
{
    [OpsRegistration(typeof(TMP_Dropdown), typeof(IDropDownOps))]
    public class TMP_DropDownOps : DropDownOps<TMP_Dropdown, TMP_Dropdown.OptionData>, IDropDownOps
    {
        [Preserve] public TMP_DropDownOps(TMP_Dropdown dropdown) : base(dropdown) { }

        public override int current { get => c.value; set => c.value = value;}

        public override List<TMP_Dropdown.OptionData> options => c.options;

        public override void InvokeChangedCallback() => c.onValueChanged.Invoke(c.value);

        public override event UnityAction<int> valueChanged
        {
            add => c.onValueChanged.AddListener(value);
            remove => c.onValueChanged.RemoveListener(new UnityAction<int>(value));
        }

        public override TMP_Dropdown.OptionData CreateData(string text) => new (text);

        public override string GetText(TMP_Dropdown.OptionData data) => data.text;
        public override void SetText(TMP_Dropdown.OptionData data, string value) => data.text = value;

        protected override void Refresh() => c.RefreshShownValue();        
    }
}
