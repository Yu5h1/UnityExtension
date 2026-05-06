#if !ENABLE_INPUT_SYSTEM
using UnityEngine;

namespace Yu5h1Lib
{
    internal class LegacyInputBackend : InputHandler.IBackend
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() => InputHandler.CurrentBackend = new LegacyBackend();

        public bool GetKeyDown(KeyCode k) => Input.GetKeyDown(k);
        public bool GetKey(KeyCode k) => Input.GetKey(k);
        public bool GetKeyUp(KeyCode k) => Input.GetKeyUp(k);
        public bool GetMouseButtonDown(int b) => Input.GetMouseButtonDown(b);
        public bool GetMouseButton(int b) => Input.GetMouseButton(b);
        public bool GetMouseButtonUp(int b) => Input.GetMouseButtonUp(b);
        public Vector2 MousePosition => Input.mousePosition;
        public float GetAxis(string name) => Input.GetAxis(name);
        public float GetAxisRaw(string name) => Input.GetAxisRaw(name);
        public bool GetButtonDown(string name) => Input.GetButtonDown(name);
        public bool GetButton(string name) => Input.GetButton(name);
        public bool GetButtonUp(string name) => Input.GetButtonUp(name);
        public Touch GetTouch(int index) => Input.GetTouch(index);
    }
}
#endif
