using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

[CreateAssetMenu(fileName = "FlexibleInputValidator", menuName = "TMP/Input Validator/Flexible")]
public class FlexibleInputValidator : TMP_InputValidator
{
    [Tooltip("列出所有不允許輸入的字元（例如 <>:{}）")]
    public string invalidChars = "";

    [Tooltip("正規表達式：只要字元符合這個表達式，就會被視為不合法。")]
    public string regexPattern = "";

    public override char Validate(ref string text, ref int pos, char ch)
    {
        // 1. 檢查 invalidChars：是否在明確禁止的字元中
        if (!string.IsNullOrEmpty(invalidChars) && invalidChars.Contains(ch.ToString()))
        {
            return '\0'; // 拒絕
        }

        // 2. 檢查 regexPattern：是否匹配非法規則
        if (!string.IsNullOrEmpty(regexPattern))
        {
            if (Regex.IsMatch(ch.ToString(), regexPattern))
            {
                return '\0'; // 匹配了非法條件 → 拒絕
            }
        }

        // 如果都沒問題 → 接受字元，並插入
        text = text.Insert(pos, ch.ToString());
        pos += 1;
        return ch;
    }
}
