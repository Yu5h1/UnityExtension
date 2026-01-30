using UnityEngine;

namespace Yu5h1Lib
{
    public class ColorObject : ParamterObject<Color>, IColor
    {
        public Color color { get => value; set => this.value = value; }
        public float alpha { get => value.a; set => this.value.a = value; }
    }
}
