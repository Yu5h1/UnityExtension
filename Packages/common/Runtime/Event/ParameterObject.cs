using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Type = System.Type;

namespace Yu5h1Lib
{
    public abstract class ParameterObject : ScriptableObject {
        public abstract Type GetValueType();
        public abstract void ApplyTo(Object target);
    }
    public abstract class ParameterObject<T> : ParameterObject
    {
        [Decorable] public T value;
        override public Type GetValueType() => typeof(T);
        public static implicit operator T(ParameterObject<T> obj) => obj.value;
        public override void ApplyTo(Object target)
        {
            if (name.IsEmpty()) return;

            var propName = name.Contains('.') ? name.Split('.').Last() : name;

            var prop = target.GetType().GetProperty(propName,
                BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanWrite) return;

            if (prop.PropertyType.IsAssignableFrom(typeof(T)))
                prop.SetValue(target, value);
        }
    }
    public abstract class CollectionObject<T> : ParameterObject<T[]>
    {
        public T Random() => value.RandomElement();
        public T GetRandomElement(params T[] excludeElements) => value.RandomElement(excludeElements); 
    }
    public static class CollectionObjectUtility {
        public static CollectionObject<TValue> ToArrayObject<TValue>(this IList<TValue> list)
        {
            Type arrayObjectType = null;

            if (typeof(TValue) == typeof(string))
                arrayObjectType = typeof(StringArrayObject);
            else if (typeof(TValue) == typeof(int))
                arrayObjectType = typeof(IntegerArrayObject);
            else
                return null;


            var instance = (CollectionObject<TValue>)ScriptableObject.CreateInstance(arrayObjectType);
            instance.value = (TValue[])System.Array.CreateInstance(typeof(TValue), list.Count);
            list.CopyTo(instance.value, 0);

            return instance;
        }
    }
}
