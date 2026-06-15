using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// <see cref="IRandomSource"/> backed by <c>UnityEngine.Random</c> (global state, controllable via
    /// <c>Random.InitState</c>). Inject into a <see cref="RandomResolver"/> to use Unity's RNG instead of System.Random.
    /// </summary>
    public sealed class UnityRandomSource : IRandomSource
    {
        public static readonly UnityRandomSource Default = new UnityRandomSource();

        // UnityEngine.Random.Range(int,int) is max-exclusive; Range(float,float) is max-inclusive.
        public int Next(int minInclusive, int maxExclusive) => Random.Range(minInclusive, maxExclusive);
        public float NextFloat(float minInclusive, float maxInclusive) => Random.Range(minInclusive, maxInclusive);
    }
}
