using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>Observes the value, focus and availability of a native uGUI InputField.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(InputField))]
    public sealed class InputFieldState : MonoBehaviour
    {
        private InputField input;
        private (string value, bool focused, bool interactable) previous;

        private InputField Input => input != null ? input : input = GetComponent<InputField>();

        public string Value => Input.text;
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool HasNonWhitespace => !string.IsNullOrWhiteSpace(Value);
        public bool IsInteractable => Input.IsActive() && Input.IsInteractable();
        public bool IsFocused => IsInteractable &&
            (Input.isFocused || EventSystem.current?.currentSelectedGameObject == gameObject);

        /// <summary>Reports changes to the observed state, including values assigned without input notifications.</summary>
        public event Action<InputFieldState> Changed;

        private void OnEnable()
        {
            previous = Capture();
            Input.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDisable() => Input.onValueChanged.RemoveListener(OnValueChanged);
        private void LateUpdate() => RefreshState();
        private void OnValueChanged(string _) => RefreshState();

        public void RefreshState()
        {
            if (!isActiveAndEnabled)
                return;

            var current = Capture();
            if (current == previous)
                return;

            previous = current;
            Changed?.Invoke(this);
        }

        private (string value, bool focused, bool interactable) Capture() =>
            (Value, IsFocused, IsInteractable);
    }
}
