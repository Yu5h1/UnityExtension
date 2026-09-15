using UnityEngine;

public static class MathEx
{
	public static float GetNormal(this float val,float max, float zeroMaxResult = 0) => max == 0 ? zeroMaxResult : val / max;

	public static float Distance(this float a, float b) => Mathf.Abs(b - a); //Mathf.Sqrt(Mathf.Pow(b - a,2));

	/// <summary>Neither NaN nor infinite. Guards arithmetic fed by pointer deltas, camera state or layout.</summary>
	public static bool IsFinite(this float value) => float.IsFinite(value);

	/// <inheritdoc cref="IsFinite(float)"/>
	public static bool IsFinite(this Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);

	/// <inheritdoc cref="IsFinite(float)"/>
	public static bool IsFinite(this Vector3 value)
		=> float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

	/// <summary>Every component is finite. Says nothing about size, so a zero-area rect still passes.</summary>
	public static bool IsFinite(this Rect value)
		=> float.IsFinite(value.x) && float.IsFinite(value.y)
		&& float.IsFinite(value.width) && float.IsFinite(value.height);

	/// <summary>
	/// Finite <em>and</em> big enough to divide by. Use this before normalising a point against a rect;
	/// use <see cref="IsFinite(Rect)"/> when a caller checks the size itself.
	/// </summary>
	public static bool IsValid(this Rect value)
		=> value.IsFinite() && value.max.IsFinite() && value.width > 0 && value.height > 0;
}
