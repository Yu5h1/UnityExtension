using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Type = System.Type;
using Exception = System.Exception;

namespace Yu5h1Lib
{
    public abstract class ParameterObject : ScriptableObject {
        public abstract Type DeclaredType { get; }
        public abstract void ApplyTo(Object target);
    }
    public abstract class ParameterObject<T> : ParameterObject
    {
        [Decorable,Inline(true)] public T value;

        public override Type DeclaredType => typeof(T);
        public static implicit operator T(ParameterObject<T> obj) => obj.value;


        private static readonly BindingFlags flags =
          BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        private readonly object[] _args1 = new object[1];
        // ---- OPT: global cache
        private static readonly Dictionary<SetterKey, MethodInfo> _setterCache = new();
        private readonly struct SetterKey : System.IEquatable<SetterKey>
        {
            public readonly Type targetType;
            public readonly string memberName;
            public readonly Type declaredType;

            public SetterKey(Type targetType, string memberName, Type declaredType)
            {
                this.targetType = targetType;
                this.memberName = memberName;
                this.declaredType = declaredType;
            }

            public bool Equals(SetterKey other)
                => targetType == other.targetType && memberName == other.memberName && declaredType == other.declaredType;

            public override bool Equals(object obj) => obj is SetterKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = targetType != null ? targetType.GetHashCode() : 0;
                    h = (h * 397) ^ (memberName != null ? memberName.GetHashCode() : 0);
                    h = (h * 397) ^ (declaredType != null ? declaredType.GetHashCode() : 0);
                    return h;
                }
            }
        }

        private static MethodInfo GetSetterCached(Type type, string memberName, Type declaredType)
        {
            var key = new SetterKey(type, memberName, declaredType);
            if (_setterCache.TryGetValue(key, out var cached))
                return cached; // may be null (negative cache)

            MethodInfo setter = null;

            // prop first
            var prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.PropertyType == declaredType)
            {
                var setMethod = prop.SetMethod;
                if (setMethod != null && setMethod.IsPublic)
                    setter = setMethod;
            }

            // method fallback
            if (setter == null)
            {
                var m = type.GetMethod(memberName, flags, null, new[] { declaredType }, null);
                if (m != null && m.IsPublic)
                    setter = m;
            }

            _setterCache[key] = setter; // cache (including null)
            return setter;
        }

        public override void ApplyTo(Object target)
        {
            if (string.IsNullOrEmpty(name) || target == null) return;

            var memberName = name;
            var dot = memberName.LastIndexOf('.');
            if (dot >= 0) memberName = memberName.Substring(dot + 1);

            // null guard for non-nullable value types
            if (value == null && DeclaredType.IsValueType && System.Nullable.GetUnderlyingType(DeclaredType) == null)
                return;

            var type = target.GetType();

            var setter = GetSetterCached(type, memberName, DeclaredType);
            if (setter == null) return;

            try
            {
                _args1[0] = value;
                setter.Invoke(target, _args1);
            }
            catch (Exception e)
            {
                // 這裡 expects 理論上就等於 DeclaredType（你是嚴格匹配），就不額外 GetParameters() 了
                ($"Invoke failed: '{type.Name}.{setter.Name}' expects '{DeclaredType.FullName}', " +
                 $"value '{value?.GetType().FullName ?? "null"}'. Exception: {e.GetType().Name}: {e.Message}")
                 .printWarning();
            }
            finally
            {
                _args1[0] = null; // optional: avoid holding refs
            }
        }
    }
    public abstract class ParameterCollection<T> : ParameterObject<T[]>
    {
        public Type ElementType => typeof(T);

        public T Random() => value.RandomElement();
        public T GetRandomElement(params T[] excludeElements) => value.RandomElement(excludeElements); 

    }
    public static class ParameterCollectionUtility {
        public static ParameterCollection<TValue> ToArrayObject<TValue>(this IList<TValue> list)
        {
            Type arrayObjectType = null;

            if (typeof(TValue) == typeof(string))
                arrayObjectType = typeof(StringArrayObject);
            else if (typeof(TValue) == typeof(int))
                arrayObjectType = typeof(IntegerArrayObject);
            else
                return null;


            var instance = (ParameterCollection<TValue>)ScriptableObject.CreateInstance(arrayObjectType);
            instance.value = (TValue[])System.Array.CreateInstance(typeof(TValue), list.Count);
            list.CopyTo(instance.value, 0);

            return instance;
        }
    }
}
