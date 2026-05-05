using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 跨平台輸入幫助類
/// 支援: PC (Keyboard + Mouse), Android (Gamepad + Touch), WebGL (Keyboard + Mouse)
/// 自動處理虛擬按鈕的跨平台映射
/// </summary>
public static class InputHelper
{
    private static InputActionAsset _inputActions;

    static InputHelper()
    {
        _inputActions = InputSystem.actions;
        if (_inputActions != null)
        {
            _inputActions.Enable();
        }
    }

    #region 虛擬按鈕 - 跨平台支援

    /// <summary>
    /// 虛擬 Submit 按鈕 (Enter/Space/Gamepad-South/Touch)
    /// </summary>
    public static bool Submit => GetButtonDown("Submit");

    /// <summary>
    /// 虛擬 Submit 保持按住
    /// </summary>
    public static bool SubmitHeld => GetButton("Submit");

    /// <summary>
    /// 虛擬 Cancel 按鈕 (Esc/Gamepad-East/BackButton)
    /// </summary>
    public static bool Cancel => GetButtonDown("Cancel");

    /// <summary>
    /// 虛擬 Cancel 保持按住
    /// </summary>
    public static bool CancelHeld => GetButton("Cancel");

    /// <summary>
    /// 虛擬導航 (方向鍵/WASD/Gamepad搖桿/觸屏滑動)
    /// </summary>
    public static Vector2 Navigate => GetNavigate();

    #endregion

    #region 虛擬按鈕 - 泛用方法

    /// <summary>
    /// 取得虛擬按鈕按下 (一幀)
    /// </summary>
    public static bool GetButtonDown(string buttonName)
    {
        if (_inputActions == null) return false;
        var action = _inputActions.FindAction(buttonName);
        return action != null && action.WasPressedThisFrame();
    }

    /// <summary>
    /// 取得虛擬按鈕保持按住
    /// </summary>
    public static bool GetButton(string buttonName)
    {
        if (_inputActions == null) return false;
        var action = _inputActions.FindAction(buttonName);
        return action != null && action.IsPressed();
    }

    /// <summary>
    /// 取得虛擬按鈕抬起 (一幀)
    /// </summary>
    public static bool GetButtonUp(string buttonName)
    {
        if (_inputActions == null) return false;
        var action = _inputActions.FindAction(buttonName);
        return action != null && action.WasReleasedThisFrame();
    }

    /// <summary>
    /// 取得虛擬導航軸 (Vector2)
    /// </summary>
    public static Vector2 GetNavigate()
    {
        if (_inputActions == null) return Vector2.zero;
        var action = _inputActions.FindAction("Navigate");
        return action != null ? action.ReadValue<Vector2>() : Vector2.zero;
    }

    #endregion

    #region 鍵盤實體按鍵

    /// <summary>
    /// 取得實體按鍵按下 (一幀) - PC/WebGL 專用
    /// </summary>
    public static bool GetKeyDown(Key key)
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[key].wasPressedThisFrame;
    }

    /// <summary>
    /// 取得實體按鍵保持按住 - PC/WebGL 專用
    /// </summary>
    public static bool GetKey(Key key)
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[key].isPressed;
    }

    /// <summary>
    /// 取得實體按鍵抬起 (一幀) - PC/WebGL 專用
    /// </summary>
    public static bool GetKeyUp(Key key)
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[key].wasReleasedThisFrame;
    }

    /// <summary>
    /// 取得 KeyCode 按下 (一幀) - 相容舊系統，使用預先 cache 的轉換
    /// </summary>
    public static bool GetKeyDown(KeyCode keyCode)
    {
        var key = InputSystemCompat.KeyCodeToKey(keyCode);
        return key != Key.None && GetKeyDown(key);
    }

    /// <summary>
    /// 取得 KeyCode 保持按住 - 相容舊系統，使用預先 cache 的轉換
    /// </summary>
    public static bool GetKey(KeyCode keyCode)
    {
        var key = InputSystemCompat.KeyCodeToKey(keyCode);
        return key != Key.None && GetKey(key);
    }

    /// <summary>
    /// 取得 KeyCode 抬起 (一幀) - 相容舊系統，使用預先 cache 的轉換
    /// </summary>
    public static bool GetKeyUp(KeyCode keyCode)
    {
        var key = InputSystemCompat.KeyCodeToKey(keyCode);
        return key != Key.None && GetKeyUp(key);
    }

    #endregion

    #region 常用功能鍵快捷

    /// <summary>
    /// Tab 鍵 (一幀)
    /// </summary>
    public static bool Tab => GetKeyDown(Key.Tab);

    /// <summary>
    /// Escape 鍵 (一幀)
    /// </summary>
    public static bool Escape => GetKeyDown(Key.Escape);

    /// <summary>
    /// Enter 鍵 (一幀)
    /// </summary>
    public static bool Enter => GetKeyDown(Key.Enter);

    /// <summary>
    /// Space 鍵 (一幀)
    /// </summary>
    public static bool Space => GetKeyDown(Key.Space);

    /// <summary>
    /// Shift 鍵保持按住
    /// </summary>
    public static bool ShiftHeld => GetKey(Key.LeftShift) || GetKey(Key.RightShift);

    /// <summary>
    /// Ctrl 鍵保持按住
    /// </summary>
    public static bool CtrlHeld => GetKey(Key.LeftCtrl) || GetKey(Key.RightCtrl);

    /// <summary>
    /// Alt 鍵保持按住
    /// </summary>
    public static bool AltHeld => GetKey(Key.LeftAlt) || GetKey(Key.RightAlt);

    #endregion

    #region 方向和移動

    /// <summary>
    /// 上方向鍵或 W 鍵 (一幀)
    /// </summary>
    public static bool Up => GetKeyDown(Key.UpArrow) || GetKeyDown(Key.W);

    /// <summary>
    /// 下方向鍵或 S 鍵 (一幀)
    /// </summary>
    public static bool Down => GetKeyDown(Key.DownArrow) || GetKeyDown(Key.S);

    /// <summary>
    /// 左方向鍵或 A 鍵 (一幀)
    /// </summary>
    public static bool Left => GetKeyDown(Key.LeftArrow) || GetKeyDown(Key.A);

    /// <summary>
    /// 右方向鍵或 D 鍵 (一幀)
    /// </summary>
    public static bool Right => GetKeyDown(Key.RightArrow) || GetKeyDown(Key.D);

    /// <summary>
    /// 上方向鍵或 W 鍵保持按住
    /// </summary>
    public static bool UpHeld => GetKey(Key.UpArrow) || GetKey(Key.W);

    /// <summary>
    /// 下方向鍵或 S 鍵保持按住
    /// </summary>
    public static bool DownHeld => GetKey(Key.DownArrow) || GetKey(Key.S);

    /// <summary>
    /// 左方向鍵或 A 鍵保持按住
    /// </summary>
    public static bool LeftHeld => GetKey(Key.LeftArrow) || GetKey(Key.A);

    /// <summary>
    /// 右方向鍵或 D 鍵保持按住
    /// </summary>
    public static bool RightHeld => GetKey(Key.RightArrow) || GetKey(Key.D);

    /// <summary>
    /// 取得方向輸入 (Vector2) - 支援方向鍵、WASD、Gamepad搖桿
    /// </summary>
    public static Vector2 Direction
    {
        get
        {
            var direction = Vector2.zero;

            // 方向鍵 + WASD
            if (UpHeld) direction.y += 1;
            if (DownHeld) direction.y -= 1;
            if (RightHeld) direction.x += 1;
            if (LeftHeld) direction.x -= 1;

            // 如果沒有按鍵輸入，嘗試取 Gamepad 搖桿或虛擬導航
            if (direction == Vector2.zero)
            {
                direction = Navigate;
            }

            return direction.normalized;
        }
    }

    #endregion

    #region 滑鼠輸入 (PC/WebGL)

    /// <summary>
    /// 滑鼠左鍵按下 (一幀)
    /// </summary>
    public static bool MouseLeftDown => Mouse.current?.leftButton.wasPressedThisFrame ?? false;

    /// <summary>
    /// 滑鼠左鍵保持按住
    /// </summary>
    public static bool MouseLeftHeld => Mouse.current?.leftButton.isPressed ?? false;

    /// <summary>
    /// 滑鼠左鍵抬起 (一幀)
    /// </summary>
    public static bool MouseLeftUp => Mouse.current?.leftButton.wasReleasedThisFrame ?? false;

    /// <summary>
    /// 滑鼠右鍵按下 (一幀)
    /// </summary>
    public static bool MouseRightDown => Mouse.current?.rightButton.wasPressedThisFrame ?? false;

    /// <summary>
    /// 滑鼠右鍵保持按住
    /// </summary>
    public static bool MouseRightHeld => Mouse.current?.rightButton.isPressed ?? false;

    /// <summary>
    /// 滑鼠右鍵抬起 (一幀)
    /// </summary>
    public static bool MouseRightUp => Mouse.current?.rightButton.wasReleasedThisFrame ?? false;

    /// <summary>
    /// 滑鼠中鍵按下 (一幀)
    /// </summary>
    public static bool MouseMiddleDown => Mouse.current?.middleButton.wasPressedThisFrame ?? false;

    /// <summary>
    /// 滑鼠中鍵保持按住
    /// </summary>
    public static bool MouseMiddleHeld => Mouse.current?.middleButton.isPressed ?? false;

    /// <summary>
    /// 滑鼠中鍵抬起 (一幀)
    /// </summary>
    public static bool MouseMiddleUp => Mouse.current?.middleButton.wasReleasedThisFrame ?? false;

    /// <summary>
    /// 滑鼠位置 (螢幕座標)
    /// </summary>
    public static Vector2 MousePosition => Mouse.current?.position.ReadValue() ?? Vector2.zero;

    /// <summary>
    /// 滑鼠滾輪增量
    /// </summary>
    public static Vector2 MouseScroll => Mouse.current?.scroll.ReadValue() ?? Vector2.zero;

    /// <summary>
    /// 查詢滑鼠按鍵狀態 (泛用)
    /// </summary>
    public static bool GetMouseButton(int button)
    {
        return button switch
        {
            0 => MouseLeftHeld,
            1 => MouseRightHeld,
            2 => MouseMiddleHeld,
            _ => false
        };
    }

    /// <summary>
    /// 查詢滑鼠按鍵按下 (泛用)
    /// </summary>
    public static bool GetMouseButtonDown(int button)
    {
        return button switch
        {
            0 => MouseLeftDown,
            1 => MouseRightDown,
            2 => MouseMiddleDown,
            _ => false
        };
    }

    /// <summary>
    /// 查詢滑鼠按鍵抬起 (泛用)
    /// </summary>
    public static bool GetMouseButtonUp(int button)
    {
        return button switch
        {
            0 => MouseLeftUp,
            1 => MouseRightUp,
            2 => MouseMiddleUp,
            _ => false
        };
    }

    #endregion

    #region 觸屏輸入 (Android/WebGL Touch)

    /// <summary>
    /// 觸屏數量
    /// </summary>
    public static int TouchCount
    {
        get
        {
            if (Touchscreen.current == null) return 0;
            return Touchscreen.current.press.isPressed ? 1 : 0;
        }
    }

    /// <summary>
    /// 是否有觸屏接觸
    /// </summary>
    public static bool TouchActive => Touchscreen.current?.press.isPressed ?? false;

    /// <summary>
    /// 觸屏位置 (螢幕座標)
    /// </summary>
    public static Vector2 TouchPosition => Touchscreen.current?.position.ReadValue() ?? Vector2.zero;

    /// <summary>
    /// 觸屏按下 (一幀)
    /// </summary>
    public static bool TouchDown => Touchscreen.current?.press.wasPressedThisFrame ?? false;

    /// <summary>
    /// 觸屏抬起 (一幀)
    /// </summary>
    public static bool TouchUp => Touchscreen.current?.press.wasReleasedThisFrame ?? false;

    /// <summary>
    /// 取得觸屏結構 - 相容舊的 Input.GetTouch()
    /// 目前只支援第一個觸點 (touchIndex = 0)
    /// </summary>
    public static Touch GetTouch(int touchIndex = 0)
    {
        if (touchIndex != 0 || Touchscreen.current == null)
        {
            return default;
        }

        var touch = new Touch
        {
            position = Touchscreen.current.position.ReadValue(),
            rawPosition = Touchscreen.current.position.ReadValue(),
            fingerId = 0,
            tapCount = 1,
            phase = GetTouchPhase(),
            pressure = 1f,
            maximumPossiblePressure = 1f,
            type = TouchType.Direct,
            altitudeAngle = 0f,
            azimuthAngle = 0f,
            radiusVariance = 0f,
            radius = 0f
        };

        return touch;
    }

    /// <summary>
    /// 取得觸屏位置 (相容舊的 Input.GetTouch(0).position)
    /// </summary>
    public static Vector2 GetTouchPosition(int touchIndex = 0)
    {
        if (touchIndex != 0) return Vector2.zero;
        return TouchPosition;
    }
    public static float GetAxis(string axisName)
    {
        return axisName switch
        {
            "Mouse ScrollWheel" => MouseScroll.y,
            "Horizontal" => Direction.x,
            "Vertical" => Direction.y,
            _ => 0f
        };
    }
    /// <summary>
    /// 取得觸屏的 TouchPhase
    /// </summary>
    private static UnityEngine.TouchPhase GetTouchPhase()
    {
        if (Touchscreen.current == null)
            return UnityEngine.TouchPhase.Canceled;

        if (Touchscreen.current.press.wasPressedThisFrame)
            return UnityEngine.TouchPhase.Began;
        else if (Touchscreen.current.press.wasReleasedThisFrame)
            return UnityEngine.TouchPhase.Ended;
        else if (Touchscreen.current.press.isPressed)
            return UnityEngine.TouchPhase.Moved;
        else
            return UnityEngine.TouchPhase.Canceled;
    }

    #endregion

    #region Gamepad 輸入 (Android/Console)

    /// <summary>
    /// 是否連接 Gamepad
    /// </summary>
    public static bool GamepadConnected => Gamepad.current != null;

    /// <summary>
    /// Gamepad 左搖桿輸入
    /// </summary>
    public static Vector2 GamepadLeftStick => Gamepad.current?.leftStick.ReadValue() ?? Vector2.zero;

    /// <summary>
    /// Gamepad 右搖桿輸入
    /// </summary>
    public static Vector2 GamepadRightStick => Gamepad.current?.rightStick.ReadValue() ?? Vector2.zero;

    /// <summary>
    /// Gamepad South 按鈕 (PS: Cross, Xbox: A) - 一幀
    /// </summary>
    public static bool GamepadSouthDown => Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false;

    /// <summary>
    /// Gamepad South 按鈕保持按住
    /// </summary>
    public static bool GamepadSouthHeld => Gamepad.current?.buttonSouth.isPressed ?? false;

    /// <summary>
    /// Gamepad South 按鈕抬起 - 一幀
    /// </summary>
    public static bool GamepadSouthUp => Gamepad.current?.buttonSouth.wasReleasedThisFrame ?? false;

    /// <summary>
    /// Gamepad North 按鈕 (PS: Triangle, Xbox: Y) - 一幀
    /// </summary>
    public static bool GamepadNorthDown => Gamepad.current?.buttonNorth.wasPressedThisFrame ?? false;

    /// <summary>
    /// Gamepad North 按鈕保持按住
    /// </summary>
    public static bool GamepadNorthHeld => Gamepad.current?.buttonNorth.isPressed ?? false;

    /// <summary>
    /// Gamepad North 按鈕抬起 - 一幀
    /// </summary>
    public static bool GamepadNorthUp => Gamepad.current?.buttonNorth.wasReleasedThisFrame ?? false;

    /// <summary>
    /// Gamepad East 按鈕 (PS: Circle, Xbox: B) - 一幀
    /// </summary>
    public static bool GamepadEastDown => Gamepad.current?.buttonEast.wasPressedThisFrame ?? false;

    /// <summary>
    /// Gamepad East 按鈕保持按住
    /// </summary>
    public static bool GamepadEastHeld => Gamepad.current?.buttonEast.isPressed ?? false;

    /// <summary>
    /// Gamepad East 按鈕抬起 - 一幀
    /// </summary>
    public static bool GamepadEastUp => Gamepad.current?.buttonEast.wasReleasedThisFrame ?? false;

    /// <summary>
    /// Gamepad West 按鈕 (PS: Square, Xbox: X) - 一幀
    /// </summary>
    public static bool GamepadWestDown => Gamepad.current?.buttonWest.wasPressedThisFrame ?? false;

    /// <summary>
    /// Gamepad West 按鈕保持按住
    /// </summary>
    public static bool GamepadWestHeld => Gamepad.current?.buttonWest.isPressed ?? false;

    /// <summary>
    /// Gamepad West 按鈕抬起 - 一幀
    /// </summary>
    public static bool GamepadWestUp => Gamepad.current?.buttonWest.wasReleasedThisFrame ?? false;

    /// <summary>
    /// Gamepad 左肩按鈕 (PS: L1, Xbox: LB) - 一幀
    /// </summary>
    public static bool GamepadLeftShoulderDown => Gamepad.current?.leftShoulder.wasPressedThisFrame ?? false;

    /// <summary>
    /// Gamepad 左肩按鈕保持按住
    /// </summary>
    public static bool GamepadLeftShoulderHeld => Gamepad.current?.leftShoulder.isPressed ?? false;

    /// <summary>
    /// Gamepad 右肩按鈕 (PS: R1, Xbox: RB) - 一幀
    /// </summary>
    public static bool GamepadRightShoulderDown => Gamepad.current?.rightShoulder.wasPressedThisFrame ?? false;

    /// <summary>
    /// Gamepad 右肩按鈕保持按住
    /// </summary>
    public static bool GamepadRightShoulderHeld => Gamepad.current?.rightShoulder.isPressed ?? false;

    /// <summary>
    /// Gamepad 左扳機按鈕 (PS: L2, Xbox: LT)
    /// </summary>
    public static float GamepadLeftTrigger => Gamepad.current?.leftTrigger.ReadValue() ?? 0f;

    /// <summary>
    /// Gamepad 右扳機按鈕 (PS: R2, Xbox: RT)
    /// </summary>
    public static float GamepadRightTrigger => Gamepad.current?.rightTrigger.ReadValue() ?? 0f;

    /// <summary>
    /// Gamepad D-Pad 輸入 (Vector2)
    /// </summary>
    public static Vector2Int GamepadDPad
    {
        get
        {
            if (Gamepad.current == null) return Vector2Int.zero;
            var dpad = Gamepad.current.dpad;
            return new Vector2Int(
                (dpad.right.isPressed ? 1 : 0) - (dpad.left.isPressed ? 1 : 0),
                (dpad.up.isPressed ? 1 : 0) - (dpad.down.isPressed ? 1 : 0)
            );
        }
    }

    /// <summary>
    /// Gamepad 選單按鈕 (PS: Share, Xbox: View) - 一幀
    /// </summary>
    public static bool GamepadSelectDown => Gamepad.current?.selectButton.wasPressedThisFrame ?? false;

    /// <summary>
    /// Gamepad 開始按鈕 (PS: Options, Xbox: Menu) - 一幀
    /// </summary>
    public static bool GamepadStartDown => Gamepad.current?.startButton.wasPressedThisFrame ?? false;

    /// <summary>
    /// 查詢 Gamepad 面按鈕狀態 (泛用) - 保持按住
    /// South: 0, East: 1, North: 2, West: 3
    /// </summary>
    public static bool GetGamepadButton(int button)
    {
        return button switch
        {
            0 => GamepadSouthHeld,
            1 => GamepadEastHeld,
            2 => GamepadNorthHeld,
            3 => GamepadWestHeld,
            _ => false
        };
    }

    /// <summary>
    /// 查詢 Gamepad 面按鈕按下 (泛用) - 一幀
    /// South: 0, East: 1, North: 2, West: 3
    /// </summary>
    public static bool GetGamepadButtonDown(int button)
    {
        return button switch
        {
            0 => GamepadSouthDown,
            1 => GamepadEastDown,
            2 => GamepadNorthDown,
            3 => GamepadWestDown,
            _ => false
        };
    }

    /// <summary>
    /// 查詢 Gamepad 面按鈕抬起 (泛用) - 一幀
    /// South: 0, East: 1, North: 2, West: 3
    /// </summary>
    public static bool GetGamepadButtonUp(int button)
    {
        return button switch
        {
            0 => GamepadSouthUp,
            1 => GamepadEastUp,
            2 => GamepadNorthUp,
            3 => GamepadWestUp,
            _ => false
        };
    }

    #endregion

    #region 便利方法

    /// <summary>
    /// 清空輸入狀態 (通常用於場景切換或暫停時)
    /// </summary>
    public static void ClearInput()
    {
        if (_inputActions != null)
        {
            _inputActions.Disable();
            _inputActions.Enable();
        }
    }

    /// <summary>
    /// 啟用輸入系統
    /// </summary>
    public static void EnableInput()
    {
        if (_inputActions != null)
        {
            _inputActions.Enable();
        }
    }

    /// <summary>
    /// 禁用輸入系統
    /// </summary>
    public static void DisableInput()
    {
        if (_inputActions != null)
        {
            _inputActions.Disable();
        }
    }

    /// <summary>
    /// 取得當前輸入的設備類型 (用於 UI 提示)
    /// </summary>
    public static string GetCurrentDeviceType()
    {
        if (Touchscreen.current != null && TouchActive)
            return "Touch";
        else if (Gamepad.current != null)
            return "Gamepad";
        else if (Keyboard.current != null)
            return "Keyboard";
        else
            return "Unknown";
    }

    #endregion
}