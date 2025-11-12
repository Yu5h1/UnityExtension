using UnityEngine;

namespace Yu5h1Lib
{
    public class HorizontalScopeAttribute : PropertyAttribute {

        public bool DisplayLabel { get; private set; } = false;
        public HorizontalScopeAttribute() {}

        public HorizontalScopeAttribute(bool displayLabel) {
            DisplayLabel = displayLabel;
        }
    }
}
