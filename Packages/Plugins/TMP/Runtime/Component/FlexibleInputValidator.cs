using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

[CreateAssetMenu(fileName = "FlexibleInputValidator", menuName = "TMP/Input Validator/Flexible")]
public class FlexibleInputValidator : TMP_InputValidator
{
    [Tooltip("�C�X�Ҧ������\��J���r���]�Ҧp <>:{}�^")]
    public string invalidChars = "";

    [Tooltip("���W��F���G�u�n�r���ŦX�o�Ӫ�F���A�N�|�Q�������X�k�C")]
    public string regexPattern = "";

    public override char Validate(ref string text, ref int pos, char ch)
    {
        // 1. �ˬd invalidChars�G�O�_�b���T�T��r����
        if (!string.IsNullOrEmpty(invalidChars) && invalidChars.Contains(ch.ToString()))
        {
            return '\0'; // �ڵ�
        }

        // 2. �ˬd regexPattern�G�O�_�ǰt�D�k�W�h
        if (!string.IsNullOrEmpty(regexPattern))
        {
            if (Regex.IsMatch(ch.ToString(), regexPattern))
            {
                return '\0'; // �ǰt�F�D�k���� �� �ڵ�
            }
        }

        // �p�G���S���D �� �����r���A�ô��J
        text = text.Insert(pos, ch.ToString());
        pos += 1;
        return ch;
    }
}
