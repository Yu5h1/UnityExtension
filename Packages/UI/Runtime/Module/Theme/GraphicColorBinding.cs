using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib
{
    /// <summary>
    /// 顏色目標群組 - 套用到所有 Graphic (Image, Text, RawImage 等)
    /// </summary>
    public class GraphicColorBinding : Theme.Binding<Graphic, Color>
    {
        protected override void SetValue(Graphic c, Color value) => c.color = value;
    } 
}
