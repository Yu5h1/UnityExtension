using System;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    /// <summary>
    /// ScriptableObject wrapper around a Core <see cref="Resolver{TValue}"/> held in <see cref="BehaviourObject{T}.Data"/>.
    /// Bridges the resolver's C# events to serialized <see cref="UnityEvent"/>s and delegates the
    /// <see cref="IResolver{T}"/> contract to the data.
    /// <para>Two type params are required: <typeparamref name="TData"/> must be the concrete
    /// <see cref="Resolver{TValue}"/> — so Unity can serialize <see cref="BehaviourObject{T}.Data"/> without
    /// SerializeReference, and so the C# events (which live on the base class, not the interface) are reachable.
    /// Concrete assets fix both params, e.g. <c>RepeaterObject : ResolverObject&lt;Repeater, int&gt;</c>.</para>
    /// </summary>
    public abstract class ResolverObject<TData, TValue> : BehaviourObject<TData>, IResolver<TValue>
        where TData : Resolver<TValue>, new()
    {
        /// <summary>Fired after each successful step while resolving.</summary>
        [SerializeField] private UnityEvent _resolving;

        /// <summary>Fired once, when the resolver reports it has fully resolved.</summary>
        [SerializeField] private UnityEvent _resolved;

        public Type ResultType => Data.ResultType;
        public TValue Result => Data.Result;

        public bool TryResolve(out TValue result)
        {
            LazyInitialize();
            return Data.TryResolve(out result);
        }

        public void Resolve()
        {
            LazyInitialize();
            Data.Resolve();
        }

        public void Reset() => Data.Reset();

        protected override void Initialize()
        {
            if (Data == null)
                Data = new TData();
            Data.Resolving += () => _resolving?.Invoke();
            Data.Resolved += () => _resolved?.Invoke();
        }

        object IResolver.Result => ((IResolver)Data).Result;

        bool IResolver.TryResolve(out object result)
        {
            LazyInitialize();
            return ((IResolver)Data).TryResolve(out result);
        }
    }
}
