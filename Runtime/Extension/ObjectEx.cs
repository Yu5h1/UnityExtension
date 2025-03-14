using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class ObjectEx
    {
        public static string GetNameWithOutClone<T>(this T obj) where T : Object
        {
            var name = obj.name;
            if (name.EndsWith("(Clone)"))
                name = name.Remove(name.Length - "(Clone)".Length);
            return name;
        }
        public static bool WarnningIfNullOrMissing(this Object obj)
        {
            if (obj)
                return false;
            if (ReferenceEquals(obj, null))
                $"Warning: {obj.GetType()} is MISSING! The reference was lost.".printWarning();
            else
                $"Warning: {obj.GetType()}  has NOT been assigned!".printWarning();
            return true;
        }
    }
}
