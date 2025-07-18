using System;
using System.Collections.Generic;
using UnityEngine;
using Expression = System.Linq.Expressions.Expression;


namespace Yu5h1Lib.Common
{
    public interface IOps
    {
        Component RawComponent { get; }

        //bool IsValid { get; }

        //void Refresh();
    }

    public static class OpsFactory
    {
        private static readonly Dictionary<Type, Func<Component, IOps>> _constructors = new();
        public static readonly Dictionary<Type,List<Type>> Ops = new();

        public static void Register<T,TGroup>(Func<T, IOps> constructor) where T : Component
                                                                         where TGroup : IOps
        {
            _constructors[typeof(T)] = comp => constructor((T)comp);

            var opsType = typeof(TGroup);

            if (!Ops.TryGetValue(opsType, out var components))
                components = Ops[opsType] = new List<Type>();
            if (!components.Contains(typeof(T)))
                components.Add(typeof(T));

        }

        public static Func<TComponent, TOps> CreateConstructor<TComponent, TOps>()
        {
            var param = Expression.Parameter(typeof(TComponent), "c");
            var ctor = Expression.New(typeof(TOps).GetConstructor(new[] { typeof(TComponent) }), param);
            var lambda = Expression.Lambda<Func<TComponent, TOps>>(ctor, param);
            return lambda.Compile();
        }

        public static bool TryCreate(Component component, out IOps ops)
        {
            ops = null;
            if (component == null)
                return false;

            var type = component.GetType();

            if (_constructors.TryGetValue(type, out var creator))
            {
                ops = creator(component);
                return true;
            }

            foreach (var kvp in _constructors)
            {
                if (kvp.Key.IsAssignableFrom(type))
                {
                    ops = kvp.Value(component);
                    return true;
                }
            }

            return false;
        }
        public static IOps Create(Component component) => TryCreate(component,out IOps result) ? result : null;

        public static bool TryCreate<T>(Component component, out T ops) where T : class, IOps
        {
            ops = null;
            if (!TryCreate(component, out var baseOps))
                return false;

            if (baseOps is T casted)
            {
                ops = casted;
                return true;
            }

            return false;
        }
        public static T Create<T>(Component component) where T : class, IOps
            => TryCreate(component, out T result) ? result : null;
    }

}
