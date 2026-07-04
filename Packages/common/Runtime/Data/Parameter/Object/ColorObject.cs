using UnityEngine;

namespace Yu5h1Lib
{
    [Icon("d_ColorPicker.CycleSlider")]
    public class ColorObject : ParameterObject<Color>, IColor
    {
        public Color color { get => value; set => this.value = value; }
        public float alpha { get => value.a; set => this.value.a = value; }
    }
}
