using System;
using System.ComponentModel;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class MonoScriptEx
    {
        public static bool IsSubclassOf(this MonoScript m,Type t)
        {
            var classtype = m.GetClass();
            if (classtype == null)
                return false;
            return classtype.IsSubclassOf(t);
        }
    }
}
