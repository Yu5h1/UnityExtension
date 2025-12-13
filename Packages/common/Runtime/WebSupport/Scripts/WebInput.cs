using System.Collections.Generic;
using DllImport = System.Runtime.InteropServices.DllImportAttribute;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib.WebSupport
{
    public class WebInput : SingletonBehaviour<WebInput>
    {
        [System.Serializable]
        public class KeyMessage
        {
            public string code;

            public bool shift;
            public bool ctrl;
            public bool alt;
            public bool meta;
            public override string ToString() => $"KeyMessage(code:{code}, shift:{shift}, ctrl:{ctrl}, alt:{alt}, meta:{meta})";

        }
        [System.Serializable]
        public class WebKeyMessage : KeyMessage
        {
            public string key;
            public int keyCode;

            public bool isComposing;
            public bool imeActive;

            public override string ToString() 
                => $"WebKeyMessage(code:{code}, key:{key}, keyCode:{keyCode}, shift:{shift}, ctrl:{ctrl}, alt:{alt}, meta:{meta}, isComposing:{isComposing}, imeActive:{imeActive})";
        }

        [System.Serializable]
        public class KeyBinding : KeyMessage
        {
            public bool IsPressed { get; set; }
            [SerializeField] private UnityEvent<WebKeyMessage> _up;
            public event UnityAction<WebKeyMessage> up
            {
                add => _up.AddListener(value);
                remove => _up.RemoveListener(value);
            }
            [SerializeField] private UnityEvent<WebKeyMessage> _down;
            public event UnityAction<WebKeyMessage> down
            {
                add => _down.AddListener(value);
                remove => _down.RemoveListener(value);
            }
            public bool Validate(WebKeyMessage msg)
            {
                if (msg == null || msg.isComposing || msg.imeActive)
                    return false;
                return msg.shift == shift && msg.ctrl == ctrl && msg.alt == alt && msg.meta == meta;
            }
            public void OnKeyDown(WebKeyMessage msg)
            {
                if (!Validate(msg))
                    return;
                _down?.Invoke(msg);
            }
            public void OnKeyUp(WebKeyMessage msg)
            {
                if (!Validate(msg))
                    return;
                _up?.Invoke(msg);
            }
        }
        [SerializeField] private bool _mobileKeyboardSupport = true;
        public bool mobileKeyboardSupport
        {
            get => _mobileKeyboardSupport;
            set
            {
                if (_mobileKeyboardSupport == value)
                    return;
                _mobileKeyboardSupport = value;
#if UNITY_WEBGL
                WebGLInput.mobileKeyboardSupport = value; 
#endif

            }
        }
        [SerializeField] private bool _captureAllKeyboardInput = true;
        public bool captureAllKeyboardInput
        {
            get => _captureAllKeyboardInput;
            set
            {
                if (_captureAllKeyboardInput == value)
                    return;
                _captureAllKeyboardInput = value;
#if UNITY_WEBGL
                WebGLInput.captureAllKeyboardInput = value; 
#endif
            }
        }
        public readonly Dictionary<string, KeyBinding> bindings = new Dictionary<string, KeyBinding>();
        private static readonly Dictionary<KeyCode, string> specialKeys = new Dictionary<KeyCode, string>()
{
    { KeyCode.LeftShift, "ShiftLeft" },
    { KeyCode.RightShift, "ShiftRight" },
    { KeyCode.LeftControl, "ControlLeft" },
    { KeyCode.RightControl, "ControlRight" },
    { KeyCode.LeftAlt, "AltLeft" },
    { KeyCode.RightAlt, "AltRight" },
    { KeyCode.Return, "Enter" },
    { KeyCode.KeypadEnter , "Enter" },
};

        public static KeyBinding PrepareKeyBinding(string key)
        {
            if (!instance.bindings.ContainsKey(key))
                instance.bindings[key] = new KeyBinding();

            return instance.bindings[key];
        }
        public static KeyBinding PrepareKeyBinding(KeyBinding b)
        {
            if (!instance.bindings.ContainsKey(b.code))
                instance.bindings[b.code] = b;
            return instance.bindings[b.code];
        }
        public bool registerkeylisteners = true;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void RegisterKeyListeners(string gameObjectName);
#endif
        [SerializeField] private List<KeyBinding> _keybindings;

        protected override void OnInstantiated() { }
        protected override void OnInitializing()
        {
#if UNITY_WEBGL 
            if (!Application.isEditor)
            {
#if !UNITY_EDITOR
                if (!Application.isMobilePlatform && registerkeylisteners)
                    RegisterKeyListeners(gameObject.name); 
#endif
                WebGLInput.mobileKeyboardSupport = _mobileKeyboardSupport;
                WebGLInput.captureAllKeyboardInput = _captureAllKeyboardInput;
            }
#endif

            foreach (var b in _keybindings)
                PrepareKeyBinding(b);
        }
        public void OnKeyDown(string jsonData)
        {
            if (jsonData.IsEmpty())
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                "[WebInput] Received empty key data".printWarning(); 
#endif
                return;
            }

            try
            {
                var keyMsg = JsonUtility.FromJson<WebKeyMessage>(jsonData);                
                if (keyMsg.code.IsEmpty())
                {
                    Debug.LogWarning("[WebInput] Key data has no code");
                    return;
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[WebInput] OnKeyDown: {keyMsg.code} (Shift:{keyMsg.shift}, Ctrl:{keyMsg.ctrl}, Alt:{keyMsg.alt})");
#endif
                var binding = PrepareKeyBinding(keyMsg.code);
                if (!binding.IsPressed)
                {
                    binding.IsPressed = true;
                    binding.OnKeyDown(keyMsg);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WebInput] Failed to parse key data: {e.Message}");
            }

        }

        public void OnKeyUp(string jsonKeyMessage)
        {
            if (jsonKeyMessage.IsEmpty())
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[WebInput] Received empty key data"); 
#endif
                return;
            }

            try
            {
                var keyMsg = JsonUtility.FromJson<WebKeyMessage>(jsonKeyMessage);

                if (keyMsg.code.IsEmpty())
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("[WebInput] Key data has no code"); 
#endif
                    return;
                }
                if (bindings.ContainsKey(keyMsg.code) && bindings[keyMsg.code].IsPressed)
                {
                    bindings[keyMsg.code].IsPressed = false;
                    bindings[keyMsg.code].OnKeyUp(keyMsg);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WebInput] Failed to parse key data: {e.Message}");
            }
        }

        public static bool IsKeyPressed(KeyCode key)
        {
            string code = instance.ConvertKeyCodeToJsCode(key);

            if (code.IsEmpty())
                return false;
            if (instance.bindings.TryGetValue(code, out KeyBinding b))
                return b.IsPressed;
            return false;
        }

        private string ConvertKeyCodeToJsCode(KeyCode key)
        {
            if (specialKeys.TryGetValue(key, out string code))
                return code;

            string name = key.ToString();

            // A~Z
            if (key >= KeyCode.A && key <= KeyCode.Z)
                return "Key" + name;

            // 0~9
            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
                return "Digit" + (key - KeyCode.Alpha0);

            // 
            if (key >= KeyCode.LeftArrow && key <= KeyCode.DownArrow)
                return name;

            Debug.LogWarning($"[WebGLInput] Unsupported KeyCode: {key}");
            return null;
        }

        public static bool IsShiftPressed() => IsKeyPressed(KeyCode.LeftShift) || IsKeyPressed(KeyCode.RightShift);
    }
}
