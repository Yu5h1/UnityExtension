using UnityEngine;
using Yu5h1Lib.Common;

namespace Yu5h1Lib
{
    public interface IColorOps : IOps
    {
        Color color { get; set; }
        float alpha {
            get => color.a; 
            set
            {
                color = color.SetAlpha(value);
            }
        }
    }
}
