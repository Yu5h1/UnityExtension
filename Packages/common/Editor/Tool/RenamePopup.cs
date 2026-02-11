using UnityEditor;
using UnityEngine;
using System;
using Yu5h1Lib.EditorExtension;

public class RenamePopup : PopupWindowContent
{
    public object target;
    public string[] autoFillOptions;
    private string _text;
    private string _original;
    private string _previousText;
    private Action<string> _onApply;
    private bool _focusNextFrame = true;
    private float _width;

    public static event Action<object> confirmed;
    
    public RenamePopup(string currentName, Action<string> onApply, float width = 200)
    {
        _text = currentName;
        _original = currentName;
        _previousText = currentName;
        _onApply = onApply;
        _width = width;
    }
    
    public override Vector2 GetWindowSize()
    {
        return new Vector2(_width, EditorGUIUtility.singleLineHeight + 4);
    }

    public override void OnGUI(Rect rect)
    {
        HandleKey();

        GUI.SetNextControlName("RenameInput");
        var textFieldRect = new Rect(2, 2, rect.width - 4, EditorGUIUtility.singleLineHeight);
        _text = EditorGUI.TextField(textFieldRect, _text);

        if (_focusNextFrame)
        {
            _focusNextFrame = false;
            EditorGUI.FocusTextInControl("RenameInput");
            TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            editor?.SelectAll();
        }

        // AutoFill: 文字變化時彈出候選清單
        if (autoFillOptions != null && autoFillOptions.Length > 0 && _text != _previousText)
        {
            _previousText = _text;
            var screenRect = new Rect(
                GUIUtility.GUIToScreenPoint(new Vector2(textFieldRect.x, textFieldRect.yMax)),
                new Vector2(textFieldRect.width, textFieldRect.height)
            );
            AutoFillPopup.Show(screenRect, autoFillOptions, OnAutoFillSelected, _text);
        }
    }

    private void OnAutoFillSelected(string value)
    {
        _text = value;
        _previousText = value;
    }

    private void HandleKey()
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown) return;

        // AutoFillPopup 開啟時，讓它先處理 Enter 和 Escape
        bool autoFillActive = AutoFillPopup.instance != null;

        switch (e.keyCode)
        {
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (!autoFillActive)
                {
                    ApplyAndClose();
                    e.Use();
                }
                break;
            case KeyCode.Escape:
                if (!autoFillActive)
                {
                    editorWindow.Close();
                    e.Use();
                }
                break;
        }
    }

    private void ApplyAndClose()
    {
        Apply();
        editorWindow.Close();
    }
    public override void OnClose()
    {
        Apply();
        if (AutoFillPopup.instance != null)
            AutoFillPopup.instance.Close();
    }
    private void Apply()
    {
      
        if (_text == _original)
            return;
        _onApply?.Invoke(_text.Trim());
        confirmed?.Invoke(target);
    }
}