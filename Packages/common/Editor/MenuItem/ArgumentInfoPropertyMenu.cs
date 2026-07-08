using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.EditorExtension
{
    public static class ArgumentInfoPropertyMenu
    {
        private const string MenuPath = "Paste.../UnityEvent Argument";
        private const string ObjectWrapperPrefix = "UnityEditor.ObjectWrapperJSON:";

        public static readonly string[] ArgumentInfoFieldNames =
        {
                "_targetName",
                "_argumentAssemblyTypeName",
                "_methodName",
                "_listenerMode",
                "_objectArgument",
                "_intArgument",
                "_floatArgument",
                "_stringArgument",
                "_boolArgument"
        };

        [InitializeOnLoadMethod]
        public static void Register()
        {
            EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }

        private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (!TryGetArgumentInfoPath(property, out var argumentInfoPath))
                return;

            if (!TryReadEventArgument(EditorGUIUtility.systemCopyBuffer, out var info))
            {
                menu.AddDisabledItem(new GUIContent(MenuPath));
                return;
            }

            menu.AddItem(new GUIContent(MenuPath), false, () =>
            {
                WriteArgumentInfo(property.serializedObject, argumentInfoPath, info);
            });
        }

        private static bool TryGetArgumentInfoPath(SerializedProperty property, out string argumentInfoPath)
        {
            argumentInfoPath = null;

            if (property == null)
                return false;

            if (property.type == nameof(Yu5h1Lib.ArgumentInfo))
            {
                argumentInfoPath = property.propertyPath;
                return true;
            }

            foreach (var fieldName in ArgumentInfoFieldNames)
            {
                if (!property.propertyPath.EndsWith("." + fieldName, StringComparison.Ordinal))
                    continue;

                argumentInfoPath = property.propertyPath.Substring(
                    0,
                    property.propertyPath.Length - fieldName.Length - 1);
                return true;
            }

            return false;
        }

        private static bool TryReadEventArgument(string text, out ArgumentInfo info)
        {
            info = new ArgumentInfo();

            if (!GenericPropertyJsonParser.TryGetJson(text, out _))
                return false;

            var hasValue = false;

            if (GenericPropertyJsonParser.TryGetValue(text, "m_Target", out var targetValue))
            {
                var target = ParseObjectWrapper(targetValue);
                if (target != null)
                {
                    info.targetName = target.name;
                    hasValue = true;
                }
            }

            if (GenericPropertyJsonParser.TryGetValue(text, "m_ObjectArgumentAssemblyTypeName", out var objectArgumentTypeName))
            {
                info.argumentAssemblyTypeName = objectArgumentTypeName;
                hasValue = true;
            }

            if (GenericPropertyJsonParser.TryGetValue(text, "m_MethodName", out var methodName))
            {
                info.methodName = methodName;
                hasValue = true;
            }

            if (GenericPropertyJsonParser.TryGetValue(text, "m_Mode", out var modeValue) &&
                TryParsePersistentListenerMode(modeValue, out var mode))
            {
                info.listenerMode = mode;
                hasValue = true;
            }

            if (GenericPropertyJsonParser.TryGetValue(text, "m_ObjectArgument", out var objectArgumentValue))
            {
                info.objectArgument = ParseObjectWrapper(objectArgumentValue);
                hasValue = true;
            }

            if (GenericPropertyJsonParser.TryGetValue(text, "m_IntArgument", out var intValue) &&
                int.TryParse(intValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intArgument))
            {
                info.intArgument = intArgument;
                hasValue = true;
            }

            if (GenericPropertyJsonParser.TryGetValue(text, "m_FloatArgument", out var floatValue) &&
                float.TryParse(floatValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatArgument))
            {
                info.floatArgument = floatArgument;
                hasValue = true;
            }

            if (GenericPropertyJsonParser.TryGetValue(text, "m_StringArgument", out var stringArgument))
            {
                info.stringArgument = stringArgument;
                hasValue = true;
            }

            if (GenericPropertyJsonParser.TryGetValue(text, "m_BoolArgument", out var boolValue) &&
                bool.TryParse(boolValue, out var boolArgument))
            {
                info.boolArgument = boolArgument;
                hasValue = true;
            }

            return hasValue;
        }

        private static bool TryParsePersistentListenerMode(string value, out PersistentListenerMode mode)
        {
            mode = default(PersistentListenerMode);

            if (string.IsNullOrEmpty(value))
                return false;

            const string enumPrefix = "Enum:";
            if (value.StartsWith(enumPrefix, StringComparison.Ordinal))
                value = value.Substring(enumPrefix.Length);

            return Enum.TryParse(value, out mode);
        }

        private static Object ParseObjectWrapper(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                !value.StartsWith(ObjectWrapperPrefix, StringComparison.Ordinal))
                return null;

            try
            {
                var json = value.Substring(ObjectWrapperPrefix.Length);
                var wrapper = JsonUtility.FromJson<ObjectWrapperJson>(json);
                if (wrapper == null)
                    return null;

                var instanceObject = EditorUtility.EntityIdToObject(wrapper.instanceID);
                if (instanceObject != null)
                    return instanceObject;

                if (string.IsNullOrEmpty(wrapper.guid))
                    return null;

                var path = AssetDatabase.GUIDToAssetPath(wrapper.guid);
                if (string.IsNullOrEmpty(path))
                    return null;

                if (wrapper.localId == 0)
                    return AssetDatabase.LoadMainAssetAtPath(path);

                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset == null)
                        continue;

                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out _, out long localId) &&
                        localId == wrapper.localId)
                        return asset;
                }

                return AssetDatabase.LoadMainAssetAtPath(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to parse ObjectWrapperJSON. {exception.Message}");
                return null;
            }
        }

        private static void WriteArgumentInfo(SerializedObject serializedObject, string argumentInfoPath, ArgumentInfo info)
        {
            serializedObject.Update();

            SetString(serializedObject, argumentInfoPath, "_targetName", info.targetName);
            SetString(serializedObject, argumentInfoPath, "_argumentAssemblyTypeName", info.argumentAssemblyTypeName);
            SetString(serializedObject, argumentInfoPath, "_methodName", info.methodName);
            SetEnum(serializedObject, argumentInfoPath, "_listenerMode", info.listenerMode);
            SetObject(serializedObject, argumentInfoPath, "_objectArgument", info.objectArgument);
            SetInt(serializedObject, argumentInfoPath, "_intArgument", info.intArgument);
            SetFloat(serializedObject, argumentInfoPath, "_floatArgument", info.floatArgument);
            SetString(serializedObject, argumentInfoPath, "_stringArgument", info.stringArgument);
            SetBool(serializedObject, argumentInfoPath, "_boolArgument", info.boolArgument);

            serializedObject.ApplyModifiedProperties();
        }

        private static void SetString(SerializedObject serializedObject, string rootPath, string fieldName, string value)
        {
            var property = serializedObject.FindProperty($"{rootPath}.{fieldName}");
            if (property != null)
                property.stringValue = value ?? string.Empty;
        }

        private static void SetEnum(SerializedObject serializedObject, string rootPath, string fieldName, PersistentListenerMode value)
        {
            var property = serializedObject.FindProperty($"{rootPath}.{fieldName}");
            var enumIndex = property != null
                ? Array.IndexOf(property.enumNames, value.ToString())
                : -1;

            if (enumIndex >= 0)
                property.enumValueIndex = enumIndex;
        }

        private static void SetObject(SerializedObject serializedObject, string rootPath, string fieldName, Object value)
        {
            var property = serializedObject.FindProperty($"{rootPath}.{fieldName}");
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static void SetInt(SerializedObject serializedObject, string rootPath, string fieldName, int value)
        {
            var property = serializedObject.FindProperty($"{rootPath}.{fieldName}");
            if (property != null)
                property.intValue = value;
        }

        private static void SetFloat(SerializedObject serializedObject, string rootPath, string fieldName, float value)
        {
            var property = serializedObject.FindProperty($"{rootPath}.{fieldName}");
            if (property != null)
                property.floatValue = value;
        }

        private static void SetBool(SerializedObject serializedObject, string rootPath, string fieldName, bool value)
        {
            var property = serializedObject.FindProperty($"{rootPath}.{fieldName}");
            if (property != null)
                property.boolValue = value;
        }

        [Serializable]
        private class ObjectWrapperJson
        {
            public string guid;
            public long localId;
            public int type;
            public int instanceID;
        }
    }
}
