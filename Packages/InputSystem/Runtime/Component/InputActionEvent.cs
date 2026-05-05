using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Yu5h1Lib.Input
{
    public enum InputActionsTriggerMode
    {
        /// <summary> Triggers when any single action is performed. </summary>
        Any,
        /// <summary> Triggers when all actions are active simultaneously. </summary>
        All
    }

    [DisallowMultipleComponent]
    public class InputActionEvent : BaseMonoBehaviour
    {
        [SerializeField] private InputActionReference[] actions;
        [SerializeField] private InputActionsTriggerMode mode = InputActionsTriggerMode.Any;
        [SerializeField] private UnityEvent _Triggered;

        public bool debugLog;

        // tracks which actions are currently in performed state (used by All mode)
        private readonly HashSet<InputAction> _activeActions = new HashSet<InputAction>();
        private int _validActionCount;

        void OnEnable()
        {
            if (actions == null) return;
            _validActionCount = 0;
            foreach (var a in actions)
            {
                if (a == null || a.action == null) continue;
                a.action.performed += OnPerformed;
                a.action.canceled  += OnCanceled;
                a.action.Enable();
                _validActionCount++;
            }
        }

        void OnDisable()
        {
            _activeActions.Clear();
            if (actions == null) return;
            foreach (var a in actions)
            {
                if (a == null || a.action == null) continue;
                a.action.performed -= OnPerformed;
                a.action.canceled  -= OnCanceled;
            }
        }

        private void OnPerformed(InputAction.CallbackContext ctx)
        {
            if (debugLog)
                $"[{ctx.action.name}] performed via: {ctx.control.path}".print();

            switch (mode)
            {
                case InputActionsTriggerMode.Any:
                    _Triggered?.Invoke();
                    break;

                case InputActionsTriggerMode.All:
                    _activeActions.Add(ctx.action);
                    if (_activeActions.Count >= _validActionCount)
                        _Triggered?.Invoke();
                    break;
            }
        }

        private void OnCanceled(InputAction.CallbackContext ctx)
        {
            _activeActions.Remove(ctx.action);
        }

        protected override void OnInitializing() { }
    }
}
