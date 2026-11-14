using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Runs a pointer action without taking keyboard focus from the settings panel.</summary>
    internal sealed class SettingsThemeButton : Button
    {
        private SettingsPanel panel;
        private GameObject previousSelection;

        internal void Initialize(SettingsPanel owner) => panel = owner;

        private void LateUpdate()
        {
            var current = EventSystem.current?.currentSelectedGameObject;
            previousSelection = CanRestore(current) ? current : null;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            var current = navigation;
            navigation = new Navigation { mode = Navigation.Mode.None };
            try
            {
                base.OnPointerDown(eventData);
            }
            finally
            {
                navigation = current;
            }

            var eventSystem = EventSystem.current;
            if (eventData.button == PointerEventData.InputButton.Left && IsInteractable()
                && eventSystem != null && eventSystem.currentSelectedGameObject == null
                && eventData.eligibleForClick && CanRestore(previousSelection))
                eventSystem.SetSelectedGameObject(previousSelection, eventData);
        }

        protected override void OnDisable()
        {
            previousSelection = null;
            base.OnDisable();
        }

        private bool CanRestore(GameObject selection) => panel != null && panel.IsOpen && selection != null
            && selection.transform.IsChildOf(panel.transform)
            && selection.TryGetComponent<Selectable>(out var control) && control.IsActive() && control.IsInteractable();
    }
}
