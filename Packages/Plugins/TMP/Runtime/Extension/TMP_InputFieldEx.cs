using TMPro;
using Yu5h1Lib;
using ContentType = TMPro.TMP_InputField.ContentType;


namespace TMPro
{
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
    }
}