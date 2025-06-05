using UnityEngine;

namespace Yu5h1Lib.ColorPalettes
{
    public static class GradientColorExtensions
    {
        /// <summary>
        /// �N normalized float (0~1) �M�g����N�h�q�C�⺥�h���C
        /// </summary>
        /// <param name="t">0 �� 1 ���ȡA��ܦb���h������m</param>
        /// <param name="colors">�ܤ֨���C��A�w�q���h�`�I</param>
        /// <returns>���ȫ᪺�C��</returns>
        public static Color EvaluateGradient(this float t, params Color[] colors)
        {
            if (colors == null || colors.Length < 2)
            {
                Debug.LogWarning("EvaluateGradient �ݭn�ܤ֨���C��C");
                return Color.white;
            }

            t = Mathf.Clamp01(t); // �T�O�b 0~1 ����

            float scaledT = t * (colors.Length - 1);
            int index = Mathf.FloorToInt(scaledT);
            int nextIndex = Mathf.Min(index + 1, colors.Length - 1);

            float localT = scaledT - index;

            return Color.Lerp(colors[index], colors[nextIndex], localT);
        }
    }

}