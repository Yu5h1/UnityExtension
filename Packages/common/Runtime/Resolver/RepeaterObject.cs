using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Concrete <see cref="ResolverObject{TData, TValue}"/> asset wrapping a <see cref="Repeater"/>.
    /// Repeats a fixed number of times (or forever) and raises its UnityEvents on each step / completion.
    /// </summary>
    public class RepeaterObject : ResolverObject<Repeater, int> { }
}
