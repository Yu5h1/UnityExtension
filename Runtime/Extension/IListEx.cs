
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Yu5h1Lib;

namespace UnityEngine
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class IListEx
    {
        public static T RandomElement<T>(this IList<T> list)
            => list.IsEmpty() ? default(T) : list[Random.Range(0, list.Count)];
    }
}
