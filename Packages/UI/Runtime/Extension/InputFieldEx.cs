using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public static class InputFieldEx
    {
        public static string GetPlaceholderText(this InputField field)
        {
            if (field.placeholder is Text text)
                return text.text;
            return "";
        }
        public static void SetPlaceholderText(this InputField field,string value)
        {
            if (field.placeholder is Text text)
                text.text = value;
        }
    } 
}
