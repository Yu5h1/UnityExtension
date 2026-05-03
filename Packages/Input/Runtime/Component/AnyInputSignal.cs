using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Yu5h1Lib.Input
{
    [DisallowMultipleComponent]
    public class AnyInputSignal : BaseMonoBehaviour
    {
        [SerializeField] private InputActionReference[] actions;
        [SerializeField] private UnityEvent _Triggered;

        public bool debugLog;
        void OnEnable()
        {
            if (actions == null) return;
            foreach (var a in actions)
            {
                if (a == null || a.action == null) continue;
                a.action.performed += OnPerformed;
                a.action.Enable();
            }
        }
        void OnDisable()
        {
            foreach (var a in actions)
            {
                if (a == null || a.action == null) continue;
                a.action.performed -= OnPerformed;
            }
        }
        private void OnPerformed(InputAction.CallbackContext _)
        {
            _Triggered?.Invoke();
            if (debugLog)
                $"[{_.action.name}] triggered by: {_.control.path}".print();
        }

        protected override void OnInitializing()
        {
            
        }
    } 
}