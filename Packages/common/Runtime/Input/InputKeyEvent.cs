using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using static Yu5h1Lib.InputHandler;

namespace Yu5h1Lib
{
    public class InputKeyEvent : MonoBehaviour
    {
        [FormerlySerializedAs("keyCode")]
        public KeyCode key;
        [FormerlySerializedAs("State")]
        public PressPhase phase;
        [FormerlySerializedAs("extras")]
        public KeyCode[] whileHolding;
        [FormerlySerializedAs("Event")]
        [SerializeField] private UnityEvent _Event;
        public event UnityAction Event
        {
            add => _Event.AddListener(value);
            remove => _Event.RemoveListener(value);
        }

        private void Update()
        {
            if (Evaluate() && WhileHoldingMet())
                _Event?.Invoke();
        }

        private bool Evaluate() => phase switch
        {
            PressPhase.Down => GetKeyDown(key),
            PressPhase.Hold => GetKey(key),
            PressPhase.Up => GetKeyUp(key),
            _ => false
        };

        private bool WhileHoldingMet()
        {
            if (whileHolding == null || whileHolding.Length == 0) return true;
            foreach (var k in whileHolding)
                if (!GetKey(k)) return false;
            return true;
        }
    }
}
