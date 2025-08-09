using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Yu5h1Lib.Common;

public sealed class UIInputFieldOps : InputFieldOps<InputField> , IInputFieldOps
{
    public UIInputFieldOps(InputField input) : base(input) {}

    public override bool interactable { get => c.interactable; set => c.interactable = value; }

    public override string text { get => c.text; set => c.text = value; }
    public override string placeholder
    {
        get => (c.placeholder as Text)?.text;
        set { if (c.placeholder is Text t) t.text = value; }
    }
    public override int characterLimit { get => c.characterLimit; set => c.characterLimit = value; }

    public override bool MaskPassword
    {
        get => c.contentType == InputField.ContentType.Password;
        set => c.contentType = value ? InputField.ContentType.Password : InputField.ContentType.Standard;
    }
    

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        OpsFactory.Register<InputField,IInputFieldOps>(c => new UIInputFieldOps(c));
    }
}
