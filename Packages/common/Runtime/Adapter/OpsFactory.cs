using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
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

        public static void Register<T, TGroup>(Func<T, IOps> constructor) where T : Component
                                                                 where TGroup : IOps
        {
            _constructors[typeof(T)] = comp => constructor((T)comp);

            var opsType = typeof(TGroup);

            if (!Ops.TryGetValue(opsType, out var components))
                components = Ops[opsType] = new List<Type>();
            if (!components.Contains(typeof(T)))
                components.Add(typeof(T));

        }
        public static void Register(Type opsInterface, Type componentType,  Type opsType)
        {
            _constructors[componentType] = CreateConstructor(componentType,opsType);

            if (!Ops.TryGetValue(opsInterface, out var components))
                components = Ops[opsInterface] = new List<Type>();
            if (!components.Contains(componentType))
                components.Add(componentType);
        }
        public static Func<Component, IOps> CreateConstructor(Type componentType, Type opsType)
        {
            var ctor = opsType.GetConstructor(new[] { componentType });

            if (ctor == null)
            {
                Debug.LogError($"找不到 {opsType.Name}({componentType.Name}) 建構子");
                return null;
            }

            // 確認這裡是用 ctor.Invoke
            return comp => (IOps)ctor.Invoke(new object[] { comp });
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
        public static TIOps Create<TIOps>(Component component) where TIOps : class, IOps
            => TryCreate(component, out TIOps result) ? result : null;

        public static void Clear()
        {
            _constructors.Clear();
            Ops.Clear();
        }
    }
}
