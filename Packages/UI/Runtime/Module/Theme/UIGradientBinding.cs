using UnityEngine;
using Yu5h1Lib.UI.Effects;

namespace Yu5h1Lib.UI
{
    public class UIGradientBinding : Theme.Binding<UIGradientEffect, Gradient>
    {
        protected override void SetValue(UIGradientEffect c, Gradient value) => c.Gradient = value;
    }
}
