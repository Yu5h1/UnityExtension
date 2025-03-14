using UnityEngine;

namespace Yu5h1Lib
{
    public class ContextMenuAdvanced : PropertyAttribute
    {
        public string menuName;
        public string methodName;

        public ContextMenuAdvanced(string menuName, string methodName)
        {
            this.menuName = menuName;
            this.methodName = methodName;
        }
    }
}