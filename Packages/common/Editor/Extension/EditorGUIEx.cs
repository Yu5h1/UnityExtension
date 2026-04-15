using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
	[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
	public static class EditorGUIEx
	{
        public static bool TryModify<T>(this Editor editor, ref T value, System.Func<T, T> drawer)
        {
            var newValue = drawer(value);
            if (EqualityComparer<T>.Default.Equals(value, newValue))
                return false;
            value = newValue;
            return true;
        }
        public static bool TrySlide(this Editor editor, string label, ref float current, float left, float right) 
           => editor.TryModify(ref current, v => EditorGUILayout.Slider(label, v, left, right));
    } 
}
