using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Events;
using TMPro;
using Yu5h1Lib;

[OpsRegistration(typeof(TMP_InputField), typeof(IInputFieldOps))]
public sealed class TMP_InputFieldOps : InputFieldOps<TMP_InputField>
{
    [Preserve] public TMP_InputFieldOps(TMP_InputField input) : base(input) { }

    public override bool interactable { get => c.interactable; set => c.interactable = value; }

    public override string text { get => c.text; set => c.text = value; }
    public override string placeholder
    {
        get => (c.placeholder as TMP_Text)?.text;
        set { if (c.placeholder is TMP_Text t) t.text = value; }
    }
    public override int caretPosition { get => c.caretPosition; set => c.caretPosition = value; }

    public override int selectionAnchorPosition { get => c.selectionAnchorPosition; set => c.selectionAnchorPosition = value; }
    public override int selectionFocusPosition { get => c.selectionFocusPosition; set => c.selectionFocusPosition = value; }

    public override Component textComponent => c.textComponent;

    public override int lineType { get => (int)c.lineType; set => c.lineType = (TMP_InputField.LineType)value; }

    public override int characterLimit { get => c.characterLimit; set => c.characterLimit = value; }
    public override bool MaskPassword
    {
        get => c.contentType == TMP_InputField.ContentType.Password;
        set {            
            c.contentType = value ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            c.ForceLabelUpdate();            
        }
    }
    public override bool isFocused => c.isFocused;

    public override void ActivateInputField() => c.ActivateInputField();

    public override void DeactivateInputField() => c.DeactivateInputField();

    public override void SetTextWithoutNotify(string value) => c.SetTextWithoutNotify(value);

    public override event UnityAction<string> submit
    { 
        add => c.onSubmit.AddListener(value);
        remove => c.onSubmit.RemoveListener(value);
    }
    public override event UnityAction<string> textChanged
    {
        add => c.onValueChanged.AddListener(value);
        remove => c.onValueChanged.RemoveListener(value);
    }
    public override event UnityAction<string> endEdit
    {
        add => c.onEndEdit.AddListener(value);
        remove => c.onEndEdit.RemoveListener(value);
    }    
}
