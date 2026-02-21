using UnityEngine;

namespace  Yu5h1Lib
{
	public static class InputHandler
	{
        public static System.Func<KeyCode, bool> GetKeyDownCallback;
        public static System.Func<KeyCode, bool> GetKeyCallback;
        public static System.Func<KeyCode, bool> GetKeyUpCallback;
        public static System.Func<int, bool> GetMouseButtonDownCallback;
        public static System.Func<int, bool> GetMouseButtonUpCallback;
        public static System.Func<Vector2> GetMousePosition;
        public static System.Func<string, float> GetAxis;
        public static System.Func<string, float> GetAxisRaw;
        public static System.Func<string, bool> GetButtonDown;
        public static System.Func<string, bool> GetButtonUp;
        public static System.Func<string, bool> GetButton;
        public static System.Func<int, Touch> GetTouchCallback;

        public static bool GetMouseButtonDown(int button) => GetMouseButtonDownCallback?.Invoke(button) == true;
        public static bool GetMouseButtonUp(int button) => GetMouseButtonUpCallback?.Invoke(button) == true;

        public static Vector2 MousePosition => GetMousePosition?.Invoke() ?? Vector2.zero;


        public static Touch GetTouch(int index) => GetTouchCallback?.Invoke(index) ?? default;

        public static bool GetKeyDown(KeyCode key) => GetKeyDownCallback?.Invoke(key) == true;
        public static bool GetKey(KeyCode key) => GetKeyCallback?.Invoke(key) == true;
        public static bool GetKeyUp(KeyCode key) => GetKeyUpCallback?.Invoke(key) == true;
    } 
}
