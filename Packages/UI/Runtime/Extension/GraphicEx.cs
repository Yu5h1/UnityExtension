using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib;

public static class GraphicEx 
{
    public static void CrossFadeAlphaFixed(this Graphic img, float alpha, float duration, bool ignoreTimeScale)
    {
        img.color = img.color.ChangeAlpha(alpha == 0 ? 0:1);
        img.CrossFadeAlpha(alpha == 0 ? 1 : 0, 0f, true);
        img.CrossFadeAlpha(alpha, duration, ignoreTimeScale);
    }
}
