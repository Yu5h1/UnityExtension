using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Yu5h1Lib.Common;
using TMPro;
using UnityEngine.EventSystems;

public sealed class TMP_InputFieldOps : InputFieldOps<TMP_InputField>
{
    public TMP_InputFieldOps(TMP_InputField input) : base(input) {}

    public override bool interactable { get => c.interactable; set => c.interactable = value; }

    public override string text { get => c.text; set => c.text = value; }
    public override string placeholder
    {
        get => (c.placeholder as TMP_Text)?.text;
        set { if (c.placeholder is TMP_Text t) t.text = value; }
    }
    public override int characterLimit { get => c.characterLimit; set => c.characterLimit = value; }
    public override bool MaskPassword
    {
        get => c.contentType == TMP_InputField.ContentType.Password;
        set {
            c.contentType = value ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            c.ForceLabelUpdate();
        }
    }

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




#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#else
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
    private static void Register()
    {
        OpsFactory.Register<TMP_InputField,IInputFieldOps>(c => new TMP_InputFieldOps(c));
    }
}
