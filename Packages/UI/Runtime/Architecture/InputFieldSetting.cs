
using UnityEngine.Events;

namespace Yu5h1Lib.UI
{
    [System.Serializable]
    public class InputFieldSetting 
    {
        public bool usePasswordMask;
        public string placeHolder;
        public int characterLimit;
        public int characterValidatation;
        public UnityEvent<InputFieldAdapter> textChanged;
        public UnityEvent<InputFieldAdapter> submit;
    }
}