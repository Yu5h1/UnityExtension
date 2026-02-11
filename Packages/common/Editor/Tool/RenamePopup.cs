using UnityEditor;
using UnityEngine;
using System;

public class RenamePopup : PopupWindowContent
{
    public object target;
    private string _text;
    private string _original;
    private Action<string> _onApply;
    private bool _focusNextFrame = true;
    private float _width;

    public static event Action<object> confirmed;
    
    public RenamePopup(string currentName, Action<string> onApply, float width = 200)
    {
        _text = currentName;
        _original = currentName;
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
        _text = EditorGUI.TextField(new Rect(2, 2, rect.width - 4, EditorGUIUtility.singleLineHeight), _text);

        if (_focusNextFrame)
        {
            _focusNextFrame = false;
            EditorGUI.FocusTextInControl("RenameInput");
            TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            editor?.SelectAll();
        }
    }

    private void HandleKey()
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown) return;

        switch (e.keyCode)
        {
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                ApplyAndClose();
                e.Use();
                break;
            case KeyCode.Escape:
                editorWindow.Close();
                e.Use();
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
    }
    private void Apply()
    {
        if (_text == _original)
            return;
        _onApply?.Invoke(_text.Trim());
        confirmed?.Invoke(target);
    }
}