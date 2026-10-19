using UnityEngine;
using UnityEngine.EventSystems;

namespace Yu5h1Lib.UI
{
    /// <summary>
    /// Routes <see cref="BaseInputModule"/> input queries through <see cref="InputHandler"/>
    /// so UGUI <see cref="StandaloneInputModule"/> works across both Legacy Input and the Input System package
    /// without calling <see cref="UnityEngine.Input"/> directly.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIInputHandler : BaseInput
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            var module = GetComponent<BaseInputModule>();
            if (module != null)
            {
                module.inputOverride = this;
            }
        }

        public override string compositionString => string.Empty;

        public override IMECompositionMode imeCompositionMode
        {
            get => IMECompositionMode.Auto;
            set { }
        }

        public override Vector2 compositionCursorPos
        {
            get => Vector2.zero;
            set { }
        }

        public override bool mousePresent => true;

        public override bool GetMouseButtonDown(int button) => InputHandler.GetMouseButtonDown(button);

        public override bool GetMouseButtonUp(int button) => InputHandler.GetMouseButtonUp(button);

        public override bool GetMouseButton(int button) => InputHandler.GetMouseButton(button);

        public override Vector2 mousePosition => InputHandler.MousePosition;

        public override Vector2 mouseScrollDelta => new Vector2(0f, InputHandler.GetAxis("Mouse ScrollWheel"));

        public override bool touchSupported => false;

        public override int touchCount => 0;

        public override Touch GetTouch(int index) => InputHandler.GetTouch(index);

        public override float GetAxisRaw(string axisName) => InputHandler.GetAxisRaw(axisName);

        public override bool GetButtonDown(string buttonName) => InputHandler.GetButtonDown(buttonName);
    }
}
