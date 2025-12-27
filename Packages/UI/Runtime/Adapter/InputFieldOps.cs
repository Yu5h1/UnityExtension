using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Yu5h1Lib;
using Yu5h1Lib.Common;
using Yu5h1Lib.UI;

public interface IInputFieldOps : ISelectableOps
{
    string text { get; set; }
    string placeholder { get; set; }
    int caretPosition { get; set; }
    int selectionAnchorPosition { get; set; }
    int selectionFocusPosition { get; set; }
    Component textComponent { get; }
    TextAdapter textAdapter { get; }
    int lineCount { get; }
    int lineType { get; set; }
    int characterLimit { get; set; }
    bool MaskPassword { get; set; }
    bool isFocused { get;}
    

    void SetTextWithoutNotify(string value);
    void DeactivateInputField();
    void ActivateInputField();

    event UnityAction<string> submit;
    event UnityAction<string> textChanged;
    event UnityAction<string> endEdit;
}

public abstract class InputFieldOps<T> : SelectableOps<T>, IInputFieldOps where T : Selectable
{
    protected InputFieldOps(T component) : base(component) {}

    public abstract string text { get; set; }
    public abstract string placeholder { get; set; }
    public abstract int caretPosition { get; set; }
    public abstract int selectionAnchorPosition { get; set; }
    public abstract int selectionFocusPosition { get; set; }
    public abstract Component textComponent { get; }
    public abstract int lineType { get; set; }

    public abstract int characterLimit { get; set; }
    public abstract bool MaskPassword { get; set; }
    public abstract bool isFocused { get; }

    public abstract void ActivateInputField();
    public abstract void SetTextWithoutNotify(string value);
    public abstract void DeactivateInputField();
    public abstract event UnityAction<string> submit;
    public abstract event UnityAction<string> textChanged;
    public abstract event UnityAction<string> endEdit;

    [SerializeField, ReadOnly] private TextAdapter _textAdapter;
    public TextAdapter textAdapter 
    { 
        get
        {
            if (_textAdapter == null && textComponent != null)
            { 
                if (!textComponent.TryGetComponent(out _textAdapter))
                    _textAdapter = textComponent.gameObject.AddComponent<TextAdapter>();
            }            
            return _textAdapter;
        }
    }
    public int lineCount => textAdapter.GetLineCount();

}
