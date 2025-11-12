using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class ColorEx
    {
        public static Color SetAlpha(this Color c,float alpha)
        {
            c.a = alpha;
            return c;
        }
    }
}
