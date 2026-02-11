using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Yu5h1Lib
{
    public abstract class ParameterObject : ScriptableObject {
        public abstract void ApplyTo(Component target);
    }
    public abstract class ParameterObject<T> : ParameterObject
    { 
         public T value;
        public static implicit operator T(ParameterObject<T> obj) => obj.value;
        public override void ApplyTo(Component target)
        {
            if (name.IsEmpty()) return;

            var prop = target.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanWrite) return;

            if (prop.PropertyType.IsAssignableFrom(typeof(T)))
                prop.SetValue(target, value);
        }
    }
    public abstract class ArrayObject<T> : ParameterObject<T[]>
    {
        //[Dropdown("")]
        public T Random() => value.RandomElement();
        public T GetRandomElement(params T[] excludeElements) => value.RandomElement(excludeElements); 
    }
    public static class ArrayObjectUtility {
        public static ArrayObject<TValue> ToArrayObject<TValue>(this IList<TValue> list)
        {
            Type arrayObjectType = null;

            if (typeof(TValue) == typeof(string))
                arrayObjectType = typeof(StringArrayObject);
            else if (typeof(TValue) == typeof(int))
                arrayObjectType = typeof(IntegerArrayObject);
            else
                return null;


            var instance = (ArrayObject<TValue>)ScriptableObject.CreateInstance(arrayObjectType);
            instance.value = (TValue[])Array.CreateInstance(typeof(TValue), list.Count);
            list.CopyTo(instance.value, 0);

            return instance;
        }
    }
}
