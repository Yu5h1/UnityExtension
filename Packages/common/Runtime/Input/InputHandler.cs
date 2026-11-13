using UnityEngine;

namespace Yu5h1Lib
{
    public static class InputHandler
    {
        public interface IBackend
        {
            bool GetKeyDown(KeyCode k);
            bool GetKey(KeyCode k);
            bool GetKeyUp(KeyCode k);
            bool GetMouseButtonDown(int b);
            bool GetMouseButton(int b);
            bool GetMouseButtonUp(int b);
            Vector2 MousePosition { get; }
            Vector2 MouseDelta { get; }
            float GetAxis(string name);
            float GetAxisRaw(string name);
            bool GetButtonDown(string name);
            bool GetButton(string name);
            bool GetButtonUp(string name);
            int TouchCount { get; }
            Touch GetTouch(int index);
        }

        public static IBackend CurrentBackend { get; set; }

        public static bool GetKeyDown(KeyCode k) => CurrentBackend?.GetKeyDown(k) == true;
        public static bool GetKey(KeyCode k) => CurrentBackend?.GetKey(k) == true;
        public static bool GetKeyUp(KeyCode k) => CurrentBackend?.GetKeyUp(k) == true;
        public static bool GetMouseButtonDown(int b) => CurrentBackend?.GetMouseButtonDown(b) == true;
        public static bool GetMouseButton(int b) => CurrentBackend?.GetMouseButton(b) == true;
        public static bool GetMouseButtonUp(int b) => CurrentBackend?.GetMouseButtonUp(b) == true;
        public static Vector2 MousePosition => CurrentBackend?.MousePosition ?? Vector2.zero;

        /// <summary>
        /// How far the mouse moved since the previous frame, in pixels; positive x is right and positive y is up. It
        /// keeps reporting movement while the cursor is locked, which <see cref="MousePosition"/> does not.
        /// </summary>
        public static Vector2 MouseDelta => CurrentBackend?.MouseDelta ?? Vector2.zero;
        public static float GetAxis(string name) => CurrentBackend?.GetAxis(name) ?? 0f;
        public static float GetAxisRaw(string name) => CurrentBackend?.GetAxisRaw(name) ?? 0f;
        public static bool GetButtonDown(string name) => CurrentBackend?.GetButtonDown(name) == true;
        public static bool GetButton(string name) => CurrentBackend?.GetButton(name) == true;
        public static bool GetButtonUp(string name) => CurrentBackend?.GetButtonUp(name) == true;
        /// <summary>
        /// The number of touches this frame: fingers that are down, plus fingers that lifted this frame so a gesture can
        /// see its end. Valid indices for <see cref="GetTouch"/> are <c>0</c> to <c>TouchCount - 1</c>.
        /// </summary>
        public static int TouchCount => CurrentBackend?.TouchCount ?? 0;

        /// <summary>
        /// The touch at <paramref name="index"/> among the <see cref="TouchCount"/> touches of this frame, in a stable
        /// order while the fingers stay down; <c>default</c> outside that range. <see cref="Touch.fingerId"/> identifies
        /// the same finger across frames.
        /// </summary>
        public static Touch GetTouch(int index) => CurrentBackend?.GetTouch(index) ?? default;
    }
}
