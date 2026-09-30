using System;
using System.Collections;
using UnityEngine;

namespace Yu5h1Lib
{
    public class PlayerPrefJsonSerializer : PlayerPrefsSerializer
    {
        protected override string SerializeObject<T>(T value)
        {
            ThrowIfCollection(typeof(T));
            return JsonUtility.ToJson(value);
        }

        protected override T DeserializeObject<T>(string data, T defaultValue)
        {
            ThrowIfCollection(typeof(T));
            try
            {
                return JsonUtility.FromJson<T>(data);
            }
            catch
            {
                return defaultValue;
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
