using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Dialog : UI_DialogBase
{
    public Object component;

    public bool IsValidType(Object target) => target switch
    {
        TextMeshProUGUI tmp_text => true,
        Text UI_Text => true,
        _ => false
    };
    protected override string GetText() => component switch
    {
        TextMeshProUGUI tmp_text => tmp_text.text,
        Text UI_Text => UI_Text.text,
        _ => ""
    };

    protected override void SetText(string text) {
        switch (component)
        {
            case TextMeshProUGUI tmp_text: tmp_text.text = text; break;
            case Text UI_Text: UI_Text.text = text; break;
        }
    }

    public override int GetLineCount()
         => component switch
         {
             TextMeshProUGUI tmp_text => tmp_text.textInfo == null ? 0 : tmp_text.textInfo.lineCount,
             Text UI_Text => UI_Text.cachedTextGenerator.lineCount,
             _ => 0
         };

    private void Reset()
    {
        if (TryGetComponent(out TextMeshProUGUI textMeshProUGUI))
            component = textMeshProUGUI;
        else if (TryGetComponent(out Text UI_Text))
            component = UI_Text;
    }

    public void PerformVerbatim(Object targetComponent,string content)
    {
        if (!IsValidType(targetComponent))
            return;
        if (targetComponent == null)
            return;
        component = targetComponent;
        PerformVerbatim(content);
    }
 
}
