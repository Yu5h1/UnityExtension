using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NewIPValidator", menuName = "TMP/Input Validator/IP Validator")]
public class IPInputValidator : TMP_InputValidator
{
    public override char Validate(ref string text, ref int pos, char ch)
    {
        if ((ch >= '0' && ch <= '9') || ch == '.')
        {
            text = text.Insert(pos, ch.ToString());
            pos++;
            return ch;
        }
        return '\0';
    }
}
