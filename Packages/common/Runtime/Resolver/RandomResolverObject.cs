using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Concrete <see cref="ResolverObject{TData, TValue}"/> asset wrapping a <see cref="RandomResolver"/>.
    /// Produces random integers in <c>[min, max)</c>. Defaults to Unity's RNG via <see cref="UnityRandomSource"/>;
    /// reassign <c>Data.source</c> at runtime to swap the backend.
    /// </summary>
    [CreateAssetMenu(fileName = "RandomResolver", menuName = "Yu5h1Lib/Resolver/Random")]
    public class RandomResolverObject : ResolverObject<RandomResolver, int>
    {
        protected override void Initialize()
        {
            base.Initialize();
            Data.source = UnityRandomSource.Default;
        }
    }
}
