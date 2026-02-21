using UnityEngine;

namespace Yu5h1Lib
{
    public class ReorderableAttribute : PropertyAttribute
    {
        public bool hideAddButton;
        public bool hideRemoveButton;
        public bool hideHeader;
        public ReorderableAttribute(bool hideAddButton = false, bool hideRemoveButton = false, bool hideHeader = false)
        {
            this.hideAddButton = hideAddButton;
            this.hideRemoveButton = hideRemoveButton;
            this.hideHeader = hideHeader;
        }
    }
}
