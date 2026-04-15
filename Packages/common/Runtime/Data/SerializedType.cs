using System;
using UnityEngine;

namespace Yu5h1Lib
{
    [Serializable]
    public class SerializedType
    {
        [SerializeField, AutoFill("AssemblyTypes")] private string _assemblyQualifiedName;

        private Type _cachedType;
        public Type type
        {
            get
            {
                if (_cachedType == null && !string.IsNullOrEmpty(_assemblyQualifiedName))
                    _cachedType = Type.GetType(_assemblyQualifiedName);
                return _cachedType;
            }
        }

        public static implicit operator Type(SerializedType st) => st?.type;
    }
}
