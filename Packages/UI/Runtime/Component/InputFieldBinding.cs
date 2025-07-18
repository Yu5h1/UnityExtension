using Yu5h1Lib.UI;

public class InputFieldBinding : UI_Binding<InputFieldAdapter>
{
    public override string GetValue() => target.text;
    public override void SetValue(string value) => target.SetTextWithoutNotify(value);
}
