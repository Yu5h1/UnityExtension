using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
	public abstract class ParamterObject : ScriptableObject { }

    public abstract class ParamterObject<T> : ParamterObject
    { 
         public T value;
    }

    public abstract class ArrayObject : ParamterObject {}

    public abstract class ArrayObject<T> : ArrayObject
    {
        [Dropdown("")]
        public T[] value;

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
