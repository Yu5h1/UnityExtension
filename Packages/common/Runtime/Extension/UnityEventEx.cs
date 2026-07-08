using System.Collections;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

using StringComparison = System.StringComparison;

namespace Yu5h1Lib
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class UnityEventEx
    {
        public static void AddSafely<T>(this UnityEvent<T> e,UnityAction<T> action)
        {
            e.RemoveListener(action);
            e.AddListener(action);
        }


        #region ArgumentCache

        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo persistentCallsField = typeof(UnityEventBase).GetField("m_PersistentCalls", InstanceFields);
        private static readonly MethodInfo dirtyPersistentCallsMethod = typeof(UnityEventBase).GetMethod("DirtyPersistentCalls", InstanceFields);
        private static readonly FieldInfo callsDirtyField = typeof(UnityEventBase).GetField("m_CallsDirty", InstanceFields);

        public static bool LoadArgument<TEvent>(this TEvent unityEvent, ArgumentInfo argument)
            where TEvent : UnityEventBase
        {
            if (!TryGetPersistentCall(unityEvent, argument, out object persistentCall))
                return false;

            if (!TryLoadArgument(persistentCall, argument))
                return false;

            DirtyPersistentCalls(unityEvent);
            return true;
        }

        public static bool TryGetPersistentCall<TEvent>(
            this TEvent unityEvent,
            ArgumentInfo argument,
            out object persistentCall)
            where TEvent : UnityEventBase
        {
            persistentCall = null;

            if (unityEvent == null || argument == null || string.IsNullOrEmpty(argument.methodName))
                return false;

            if (!TryGetPersistentCalls(unityEvent, out IList calls))
                return false;

            foreach (object call in calls)
            {
                if (!IsMatch(call, argument))
                    continue;

                persistentCall = call;
                return true;
            }

            return false;
        }

        private static bool TryGetPersistentCalls(UnityEventBase unityEvent, out IList calls)
        {
            calls = null;

            object persistentCalls = persistentCallsField?.GetValue(unityEvent);
            if (persistentCalls == null)
                return false;

            FieldInfo callsField = GetField(persistentCalls, "m_Calls");
            calls = callsField?.GetValue(persistentCalls) as IList;
            return calls != null;
        }

        private static bool IsMatch(object persistentCall, ArgumentInfo argument)
        {
            if (!TryGetFieldValue(persistentCall, "m_MethodName", out string methodName))
                return false;

            if (!string.Equals(methodName, argument.methodName, StringComparison.Ordinal))
                return false;

            if (!TryGetFieldValue(persistentCall, "m_Mode", out PersistentListenerMode mode))
                return false;

            if (mode != argument.listenerMode)
                return false;

            if (string.IsNullOrEmpty(argument.targetName))
                return true;

            if (!TryGetFieldValue(persistentCall, "m_Target", out Object target))
                return false;

            return target != null && string.Equals(target.name, argument.targetName, StringComparison.Ordinal);
        }

        private static bool TryLoadArgument(object persistentCall, ArgumentInfo argument)
        {
            if (!TryGetFieldValue(persistentCall, "m_Mode", out PersistentListenerMode mode))
                return false;

            if (mode != argument.listenerMode)
                return false;

            if (!TryGetFieldValue(persistentCall, "m_Arguments", out object argumentCache))
                return false;

            switch (argument.listenerMode)
            {
                case PersistentListenerMode.Int:
                    return TrySetField(argumentCache, "m_IntArgument", argument.value);

                case PersistentListenerMode.Float:
                    return TrySetField(argumentCache, "m_FloatArgument", argument.value);

                case PersistentListenerMode.String:
                    return TrySetField(argumentCache, "m_StringArgument", argument.value);

                case PersistentListenerMode.Bool:
                    return TrySetField(argumentCache, "m_BoolArgument", argument.value);

                case PersistentListenerMode.Object:
                    return TryLoadObjectArgument(argumentCache, argument);

                default:
                    return false;
            }
        }

        private static bool TryLoadObjectArgument(object argumentCache, ArgumentInfo argument)
        {
            Object objectValue = argument.value as Object;
            if (argument.value != null && objectValue == null)
                return false;

            if (!TrySetField(argumentCache, "m_ObjectArgument", objectValue))
                return false;

            return true;
        }

        private static void DirtyPersistentCalls(UnityEventBase unityEvent)
        {
            if (dirtyPersistentCallsMethod != null)
            {
                dirtyPersistentCallsMethod.Invoke(unityEvent, null);
                return;
            }

            callsDirtyField?.SetValue(unityEvent, true);
        }

        private static bool TryGetFieldValue<T>(object instance, string fieldName, out T value)
        {
            value = default(T);

            FieldInfo field = GetField(instance, fieldName);
            if (field == null)
                return false;

            object rawValue = field.GetValue(instance);
            if (rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            return rawValue == null
                && (!typeof(T).IsValueType || System.Nullable.GetUnderlyingType(typeof(T)) != null);
        }

        private static bool TrySetField(object instance, string fieldName, object value)
        {
            FieldInfo field = GetField(instance, fieldName);
            if (field == null)
                return false;

            field.SetValue(instance, value);
            return true;
        }

        private static FieldInfo GetField(object instance, string fieldName)
        {
            return instance?.GetType().GetField(fieldName, InstanceFields);
        }


        #endregion
    }
}

