using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.Runtime
{
	public static class IListEx
	{
        public static int RandomIndex(this IList list, int skipIndex = -1)
        {
            if (list == null || list.Count == 0)
                return -1;

            if (skipIndex < 0 || skipIndex >= list.Count)
                return Random.Range(0, list.Count);

            if (list.Count == 1)
                return skipIndex == 0 ? -1 : 0; 

            int r = Random.Range(0, list.Count - 1);
            return r >= skipIndex ? r + 1 : r;
        }
    } 
}
