using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib
{
    /// <summary>
    /// 顏色目標群組 - 套用到所有 Graphic (Image, Text, RawImage 等)
    /// </summary>
    public class GraphicColorBinding : Theme.BindingObject<Graphic, Color>
    {
        /// <summary>Creates a runtime binding for a named color role and the supplied graphics.</summary>
        public static GraphicColorBinding Create(string role, params Graphic[] targets)
        {
            if (string.IsNullOrEmpty(role))
                throw new System.ArgumentException("A color role is required.", nameof(role));
            if (targets == null)
                throw new System.ArgumentNullException(nameof(targets));

            var binding = CreateInstance<GraphicColorBinding>();
            binding.name = role;
            binding.SetTargets((Graphic[])targets.Clone());
            return binding;
        }

        protected override void SetValue(Graphic c, Color value) => c.color = value;
    } 
}
