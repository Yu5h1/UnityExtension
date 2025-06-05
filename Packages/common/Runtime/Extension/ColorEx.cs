using UnityEngine;

namespace Yu5h1Lib.ColorPalettes
{
    public static class GradientColorExtensions
    {
        /// <summary>
        /// 將 normalized float (0~1) 映射到任意多段顏色漸層中。
        /// </summary>
        /// <param name="t">0 到 1 的值，表示在漸層中的位置</param>
        /// <param name="colors">至少兩個顏色，定義漸層節點</param>
        /// <returns>插值後的顏色</returns>
        public static Color EvaluateGradient(this float t, params Color[] colors)
        {
            if (colors == null || colors.Length < 2)
            {
                Debug.LogWarning("EvaluateGradient 需要至少兩個顏色。");
                return Color.white;
            }

            t = Mathf.Clamp01(t); // 確保在 0~1 之間

            float scaledT = t * (colors.Length - 1);
            int index = Mathf.FloorToInt(scaledT);
            int nextIndex = Mathf.Min(index + 1, colors.Length - 1);

            float localT = scaledT - index;

            return Color.Lerp(colors[index], colors[nextIndex], localT);
        }
    }

}