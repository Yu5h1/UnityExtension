using TMPro;
using Yu5h1Lib.UI;

public class UI_Dialog_TMP : UI_DialogBase
{
    public TextMeshProUGUI textMeshProUGUI;
    protected override string GetText() => textMeshProUGUI.text;

    protected override void SetText(string text) => textMeshProUGUI.text = text;

    public override int GetLineCount() => textMeshProUGUI.textInfo == null ? 0 : textMeshProUGUI.textInfo.lineCount;

    protected override void Reset()
    {
        base.Reset();
        if (!TryGetComponent(out textMeshProUGUI))
            textMeshProUGUI = GetComponentInChildren<TextMeshProUGUI>();
    }
 
}
