using System.Collections.Generic;
using UnityEngine;
using Type = System.Type;

namespace Yu5h1Lib
{
    public abstract class ParameterObject : ScriptableObject, IParameter
    {
        public string memberName { get => name; }
        public abstract object GetValue();
        public abstract Type DeclaredType { get; }
        public abstract void ApplyTo(Object target);
    }
    public abstract class ParameterObject<T> : ParameterObject
    {
        [Decorable,Inline(true)] public T value;
        public override object GetValue() => value;
        public override Type DeclaredType => typeof(T);
        public static implicit operator T(ParameterObject<T> obj) => obj.value;

        public override void ApplyTo(Object target)
            => ParameterMember.Apply(target, memberName, value, DeclaredType);
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
