using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    /// Describes one public instance method and its serialized parameters.
    /// The ScriptableObject name is used as the method name.
    /// </summary>
    public class MethodObject : MemberObject
    {
        [SerializeField]
        private SerializedType _targetType;

        [SerializeField, Inline(true)]
        private List<ParameterObject> _parameters = new();

        public string methodName => name;
        public Type targetType => _targetType?.type;
        public IReadOnlyList<ParameterObject> parameters => _parameters;

        public void Invoke(Object target)
            => TryInvoke(target);

        public bool TryInvoke(Object target)
        {
            var type = _targetType?.type;
            if (target == null || type == null || !type.IsInstanceOfType(target))
                return false;

            return MethodInvoker.TryInvoke(target, methodName, _parameters);
        }

        /// <summary>
        /// Inline serializable representation of one method call for use by InvocationObject.
        /// </summary>
        [Serializable]
        public class Descriptor
        {
            [SerializeField]
            private string _methodName;

            [SerializeField, Inline(true)]
            private List<ParameterObject> _parameters = new();

            public string methodName => _methodName;
            public IReadOnlyList<ParameterObject> parameters => _parameters;

            public void Invoke(Object target)
                => TryInvoke(target);

            public bool TryInvoke(Object target)
                => MethodInvoker.TryInvoke(target, _methodName, _parameters);
        }
    }
}
