using UnityEngine;

namespace Yu5h1Lib
{
    public enum FlagsOptionStyle {
        Mix,
        Option,
        All
    }
    public class FlagsOptionAttribute : PropertyAttribute
    {
        public FlagsOptionStyle optionStyle;
        public string[] alloptions;
        public FlagsOptionAttribute() { }
        public FlagsOptionAttribute(FlagsOptionStyle flagsOptionStyle) {
            optionStyle = flagsOptionStyle;
        }
    }
}
