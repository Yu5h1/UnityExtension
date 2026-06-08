using UnityEngine;

public static class MathEx
{
	public static float GetNormal(this float val,float max, float zeroMaxResult = 0) => max == 0 ? zeroMaxResult : val / max;

	public static float Distance(this float a, float b) => Mathf.Abs(b - a); //Mathf.Sqrt(Mathf.Pow(b - a,2));
}