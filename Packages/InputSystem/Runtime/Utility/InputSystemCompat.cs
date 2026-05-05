using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Yu5h1Lib;

/// <summary>
/// KeyCode 到 Input System Key 的轉換工具
/// 在靜態建構子時預先 cache 所有轉換，Update 只需查表
/// </summary>
public static class InputSystemCompat
{
    private static readonly Dictionary<KeyCode, Key> KeyCodeToKeyCache = new();

    static InputSystemCompat()
    {
        // 靜態建構子 - 只執行一次，預先 cache 所有 KeyCode
        InitializeKeyCodeCache();

    }
    [RuntimeInitializeOnLoadMethod]
    static void OnRuntimeMethodLoad()
    {
        InputHandler.GetKeyDownCallback = InputHelper.GetKeyDown;
        InputHandler.GetKeyCallback = InputHelper.GetKey;
        InputHandler.GetKeyUpCallback = InputHelper.GetKeyUp;
        InputHandler.GetMouseButtonDownCallback = InputHelper.GetMouseButtonDown;
        InputHandler.GetMouseButtonUpCallback = InputHelper.GetMouseButtonUp;
        InputHandler.GetMousePosition = () => InputHelper.MousePosition;
        InputHandler.GetAxis = InputHelper.GetAxis;
        InputHandler.GetAxisRaw = InputHelper.GetAxis;
        InputHandler.GetButtonDown = InputHelper.GetButtonDown;
        InputHandler.GetButtonUp = InputHelper.GetButtonUp;
        InputHandler.GetButton = InputHelper.GetButton;
        InputHandler.GetTouchCallback = InputHelper.GetTouch;
    }

    private static void InitializeKeyCodeCache()
    {
        List<string> msgList = new();
        foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (keyCode.TryConvertKeyCode(out Key key,out string keyName))
                KeyCodeToKeyCache[keyCode] = key;
            else
                msgList.Add($"無法轉換 KeyCode.{keyCode} 到 Key (嘗試名稱: {keyName})");
        }
        if (msgList.Count > 0)
        {
            Debug.LogWarning($"以下 KeyCode 無法轉換到 Key:\n{string.Join("\n", msgList)}");
        }
    }

    /// <summary>
    /// 轉換單個 KeyCode 到 Key
    /// </summary>
    private static bool TryConvertKeyCode(this KeyCode keyCode, out Key key,out string keyName)
    {
        key = Key.None; // 預設為 None
        string keyCodeName = keyCode.ToString();

        // 特殊對應 - KeyCode 和 Key enum 名稱不同的情況
        keyName = keyCodeName switch
        {
            "Return" => "Enter",
            "LeftControl" => "LeftCtrl",
            "RightControl" => "RightCtrl",
            "LeftAlt" => "LeftAlt",
            "RightAlt" => "RightAlt",
            "LeftCommand" => "LeftMeta",
            "RightCommand" => "RightMeta",
            "Alpha0" => "Digit0",
            "Alpha1" => "Digit1",
            "Alpha2" => "Digit2",
            "Alpha3" => "Digit3",
            "Alpha4" => "Digit4",
            "Alpha5" => "Digit5",
            "Alpha6" => "Digit6",
            "Alpha7" => "Digit7",
            "Alpha8" => "Digit8",
            "Alpha9" => "Digit9",
            "Keypad0" => "Numpad0",
            "Keypad1" => "Numpad1",
            "Keypad2" => "Numpad2",
            "Keypad3" => "Numpad3",
            "Keypad4" => "Numpad4",
            "Keypad5" => "Numpad5",
            "Keypad6" => "Numpad6",
            "Keypad7" => "Numpad7",
            "Keypad8" => "Numpad8",
            "Keypad9" => "Numpad9",
            "KeypadPeriod" => "NumpadPeriod",
            "KeypadDivide" => "NumpadDivide",
            "KeypadMultiply" => "NumpadMultiply",
            "KeypadMinus" => "NumpadMinus",
            "KeypadPlus" => "NumpadPlus",
            "KeypadEnter" => "NumpadEnter",
            "KeypadEquals" => "NumpadEquals",
            "UpArrow" => "UpArrow",
            "DownArrow" => "DownArrow",
            "LeftArrow" => "LeftArrow",
            "RightArrow" => "RightArrow",
            "PageUp" => "PageUp",
            "PageDown" => "PageDown",
            "Home" => "Home",
            "End" => "End",
            "Insert" => "Insert",
            "Delete" => "Delete",
            "Backspace" => "Backspace",
            "Space" => "Space",
            "Tab" => "Tab",
            "Escape" => "Escape",
            "CapsLock" => "CapsLock",
            "NumLock" => "NumLock",
            "ScrollLock" => "ScrollLock",
            "PrintScreen" => "PrintScreen",
            "Pause" => "Pause",
            "Menu" => "ContextMenu",
            _ => keyCodeName  // 其他直接使用原名稱
        };

        return System.Enum.TryParse(keyName, out key);
    }

    /// <summary>
    /// 取得轉換後的 Key - O(1) 查表，適合在 Update 中使用
    /// </summary>
    public static Key KeyCodeToKey(KeyCode keyCode)
    {
        if (KeyCodeToKeyCache.TryGetValue(keyCode, out var key))
        {
            return key;
        }

        Debug.LogWarning($"KeyCode {keyCode} 未在 cache 中找到");
        return Key.None;
    }

    /// <summary>
    /// 檢查轉換是否成功 (用於驗證)
    /// </summary>
    public static bool IsKeyCodeValid(KeyCode keyCode)
    {
        return KeyCodeToKeyCache.TryGetValue(keyCode, out var key) && key != Key.None;
    }
}
