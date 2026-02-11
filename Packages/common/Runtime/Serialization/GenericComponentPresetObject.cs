using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Yu5h1Lib
{
    [Icon("d_Preset.Context")]
    public class GenericComponentPresetObject : ParameterObject<GenericComponentPreset> { }
    [Serializable]
    public class GenericComponentPreset : ComponentPreset<Component>
    {
        
        public SerializedAssembly targetAssembly;
        public SerializedType targetType;
        public List<PropertyValue> properties;

        public override void ApplyTo(Component target)
        {
            if ($"{targetType} is unassigned.".printWarningIf(targetType.type == null) || $"{targetType.type} not matched {target.GetType()}".printWarningIf(!targetType.type.IsInstanceOfType(target)))
                return;

            foreach (var prop in properties)
                prop.ApplyTo(target);
        }
    }
    [Serializable]
    public class PropertyValue
    {

        [AutoFill("ComponentProperties")] public string propertyName;
        [Inline] public ParameterObject value;

        public void ApplyTo(Component target)
        {
            if (value == null || string.IsNullOrEmpty(propertyName)) return;

            var prop = target.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanWrite) return;

            var valueField = value.GetType().GetField("value",
                BindingFlags.Public | BindingFlags.Instance);
            if (valueField != null && prop.PropertyType.IsAssignableFrom(valueField.FieldType))
                prop.SetValue(target, valueField.GetValue(value));
        }
    }
}
