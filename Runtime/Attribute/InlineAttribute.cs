using UnityEngine;

namespace Yu5h1Lib
{
    /// Marks a property as an inline extension anchor.
    /// Additional drawer content registered in the inline
    /// draw registry will be rendered directly beneath
    /// this property field.
    public class InlineAttribute : PropertyAttribute 
	{
		public bool Minimize = false;
        public bool ShowLabel = true;

		public InlineAttribute() {}

        public InlineAttribute(bool minimize,bool showLabel = true)
		{
			Minimize = minimize;
            ShowLabel = showLabel;
        }

    } 
}
