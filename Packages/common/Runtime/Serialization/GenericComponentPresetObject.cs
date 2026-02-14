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
        [Inline(true), StringOptionsContext("ComponentProperties")]
        public List<ParameterObject> properties;

        public override bool ApplyTo(Component component)
        {
            if ($"{component.name} {targetAssembly.name}. {targetType} is unassigned.".printWarningIf(targetType.type == null) ||
                $"{targetType.type} not matched {component.GetType()}".printWarningIf(!targetType.type.IsInstanceOfType(component)))
                return false;

            foreach (var prop in properties)
                prop.ApplyTo(component);

            return true;
        }
    }
}
