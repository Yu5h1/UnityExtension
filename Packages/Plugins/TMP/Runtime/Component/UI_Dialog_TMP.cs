using TMPro;
using UnityEngine;
using Yu5h1Lib.UI;

public class UI_Dialog_TMP : UI_DialogBase
{
    public TextMeshProUGUI textMeshProUGUI;
    protected override string GetText() => textMeshProUGUI.text;
    protected override void SetText(string text) => textMeshProUGUI.text = text;
    protected override Color GetColor() => textMeshProUGUI.color;
    protected override void SetColor(Color color)
    {
        if (textMeshProUGUI.color == color)
            return;
        textMeshProUGUI.color = color;
    }

    public override int GetLineCount() => textMeshProUGUI.textInfo == null ? 0 : textMeshProUGUI.textInfo.lineCount;

    protected override void OnInitializing()
    {
        base.OnInitializing();
        if (!TryGetComponent(out textMeshProUGUI))
            textMeshProUGUI = GetComponentInChildren<TextMeshProUGUI>();
    }


}
