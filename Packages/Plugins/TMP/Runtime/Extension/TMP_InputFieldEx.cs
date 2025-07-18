using System.ComponentModel;
using TMPro;
using Yu5h1Lib;
using ContentType = TMPro.TMP_InputField.ContentType;


namespace TMPro
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class TMP_InputFieldEx
    {
        public static void SwapContentType(this TMP_InputField field, ContentType a, ContentType b)
        {
            if ($"field.contentType not the one of {a} and {b}".printWarningIf(!field.contentType.HasAnyFlags(a, b)))
                return;
            field.contentType = field.contentType.Equals(b) ? a : b;
            field.ForceLabelUpdate();
        }
        public static void SwapContentType(this TMP_InputField field, ContentType a)
            => field.SwapContentType(a,ContentType.Standard);

        public static string GetPlaceholderText(this TMP_InputField field)
        {
            if (field.placeholder is TMP_Text text)
                return text.text;
            return "";
        }
        public static void SetPlaceholderText(this TMP_InputField field, string value)
        {
            if (field.placeholder is TMP_Text text)
                text.text = value;
        }
    }
}
