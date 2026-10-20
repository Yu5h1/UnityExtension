using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Yu5h1Lib.MVVM;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    /// Writes the current value of a value port into a public property of the target passed to
    /// <see cref="ApplyTo"/>. Created inline in a scene, it can reference a scene port, so a UnityEvent can
    /// call <see cref="ApplyTo"/> to read the port at invocation time.
    /// The value is read typed from an implemented <c>IGetter&lt;T&gt;</c>; otherwise the port's string is
    /// converted to <see cref="Preferences.GetPortValueType"/>.
    /// </summary>
    public class ValuePortApplyObject : MemberObject, IParameter
    {
        [SerializeField] private string _propertyName;
        [SerializeField, TypeRestriction(typeof(Component), filter = typeof(ValuePortResolver))]
        private Object _valueObject;

        [NonSerialized] private Object _resolvedFrom;
        [NonSerialized] private IValuePort _port;

        private static readonly Dictionary<Type, MethodInfo> GetterCache = new();

        public string memberName => _propertyName;

        private IValuePort port
        {
            get
            {
                if (_resolvedFrom != _valueObject)
                {
                    _resolvedFrom = _valueObject;
                    _port = _valueObject == null ? null : ValuePortResolver.Resolve(_valueObject);
                }
                return _port;
            }
        }

        public void ApplyTo(Object target)
        {
            if (port == null)
            {
                $"{name}: '{_valueObject}' is not a value port; nothing applied.".printWarning();
                return;
            }
            if (TryGetValue(port, out var value, out var type))
                PropertySetter.Set(target, _propertyName, value, type);
            else
                $"{name}: cannot convert '{port.GetValue()}' to {type.Name}; nothing applied.".printWarning();
        }

        public object GetValue()
            => port != null && TryGetValue(port, out var value, out _) ? value : null;

        private static bool TryGetValue(IValuePort port, out object value, out Type type)
        {
            var getter = GetTypedGetter(port.GetType());
            if (getter != null)
            {
                type = getter.ReturnType;
                value = getter.Invoke(port, null);
                return true;
            }
            type = Preferences.GetPortValueType(port);
            return PlayerPrefsSerializer.Default.TryDeserialize(port.GetValue(), type, out value);
        }

        /// <summary>The <c>Get</c> of the first <c>IGetter&lt;T&gt;</c> the port type implements, or null.</summary>
        private static MethodInfo GetTypedGetter(Type portType)
        {
            if (GetterCache.TryGetValue(portType, out var cached))
                return cached;
            MethodInfo getter = null;
            foreach (var type in portType.GetInterfaces())
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGetter<>))
                {
                    getter = type.GetMethod(nameof(IGetter<object>.Get));
                    break;
                }
            }
            return GetterCache[portType] = getter;
        }
    }
}
