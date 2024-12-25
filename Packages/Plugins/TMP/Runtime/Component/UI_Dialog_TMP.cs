using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Dialog_TMP : UI_DialogBase
{
    public TextMeshProUGUI textMeshProUGUI;
    protected override string GetText() => textMeshProUGUI.text;

    protected override void SetText(string text) => textMeshProUGUI.text = text;

    public override int GetLineCount() => textMeshProUGUI.textInfo == null ? 0 : textMeshProUGUI.textInfo.lineCount;

    private void Reset()
    {
        if (!TryGetComponent(out textMeshProUGUI))
            textMeshProUGUI = GetComponentInChildren<TextMeshProUGUI>();
    }

    //protected override void Start()
    //{
    //    base.Start();
    //    textMeshProUGUI.OnPreRenderText += TextMeshProUGUI_OnPreRenderText;
        
    //}
    //private void TextMeshProUGUI_OnPreRenderText(TMP_TextInfo obj)
    //{
        
    //}
}
