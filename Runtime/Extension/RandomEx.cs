
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Yu5h1Lib
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class RandomEx
    {
        public static T RandomElement<T>(this IEnumerable<T> enumerable, params T[] excludeElements)
        {
            if (enumerable.IsEmpty())
                return default;

            var candidates = (enumerable is IList<T> list) ? list : enumerable.ToArray();

            if (excludeElements.IsEmpty())
            {
                if (candidates.Count == 1)
                    return candidates[0];

                return candidates[Random.Range(0, candidates.Count)];
            }

            var filteredCandidates = candidates.Where(x => !excludeElements.Contains(x)).ToArray();

            if (filteredCandidates.Length == 0)
                return default;

            if (filteredCandidates.Length == 1)
                return filteredCandidates[0];

            return filteredCandidates[Random.Range(0, filteredCandidates.Length)];
        }

    }
}
