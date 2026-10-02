using System;
using System.Collections;
using UnityEngine;

namespace Yu5h1Lib
{
    public class PlayerPrefJsonSerializer : PlayerPrefsSerializer
    {
        protected override string SerializeObject(object value, Type type)
        {
            ThrowIfCollection(type);
            return JsonUtility.ToJson(value);
        }

        protected override bool TryDeserializeObject(string data, Type type, out object value)
        {
            ThrowIfCollection(type);
            try
            {
                value = JsonUtility.FromJson(data, type);
                return value != null;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        // JsonUtility 最外層只接受物件；陣列、List<T>、Dictionary 會寫成 {}，資料悄悄遺失
        private static void ThrowIfCollection(Type type)
        {
            if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
                throw new NotSupportedException(
                    $"{nameof(PlayerPrefJsonSerializer)} cannot store {type} at top level. Wrap it in a [Serializable] class.");
        }
    }
}
