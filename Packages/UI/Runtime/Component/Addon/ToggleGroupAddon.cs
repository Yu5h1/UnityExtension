using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent]
    public class ToggleGroupAddon : UI_Addon<ToggleGroup>
    {
        [SerializeField]
        private UnityEvent<Toggle> _selectedChanged;
        public event UnityAction<Toggle> SelectedChanged
        {
            add => _selectedChanged.AddListener(value);
            remove => _selectedChanged.RemoveListener(value);
        }
        public int currentIndex => ui.GetFirstActiveToggle().transform.GetSiblingIndex();

        public Toggle[] toggles { get; private set; }

        public void FindToggles()
        {
            if ("Toggle Group is unassigned".printWarningIf(ui == null))
                return;
            toggles = ui.GetComponentsInChildren<Toggle>().Where(t => t.group == ui).ToArray();
        }
        protected override void OnInitializing()
        {
            base.OnInitializing();
            FindToggles();
            for (int i = 0; i < toggles.Length; i++)
            {
                var t = toggles[i];
                t.onValueChanged.AddListener((isOn) => OnToggleValueChanged(t, isOn));
            }
        }
        private void OnToggleValueChanged(Toggle sender, bool isOn)
        {
            if (!isActiveAndEnabled)
                return;
            if (isOn)
                _selectedChanged?.Invoke(sender);
        }
        public void SetIsOnWithoutNotify(int index)
        {
            if ("SetIsOnWithoutNotify failed.index out of range".printWarningIf(!toggles.IsValid(index)))
                return;
            toggles[index].SetIsOnWithoutNotify(true);
        }

        [ContextMenu(nameof(RegisterToggleChildren))]
        public void RegisterToggleChildren()
        { 
            if ("Toggle Group is unassigned".printWarningIf(ui == null))
                return;
            toggles = ui.GetComponentsInChildren<Toggle>();
            for (int i = 0; i < toggles.Length; i++)
                toggles[i].group = ui;
        }
        [ContextMenu(nameof(InvokeTogglesOnValueChanged))]
        public void InvokeTogglesOnValueChanged()
        {
            if ("Toggle Group is unassigned".printWarningIf(ui == null))
                return;
            if (toggles.IsEmpty())
                return;
            foreach (var t in toggles)
                t.onValueChanged?.Invoke(t.isOn);
        }
    }
}