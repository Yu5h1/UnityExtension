using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
	[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
	public static class Vector2Ex
	{
		public static Vector2 Swap (this Vector2 v) => new Vector2(v.y, v.x);
    } 
}
