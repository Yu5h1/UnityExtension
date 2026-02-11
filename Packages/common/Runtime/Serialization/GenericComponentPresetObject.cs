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
        [Tooltip("Override the GameObject active state.")]
        public Optional<bool> activeSelf;
        public SerializedAssembly targetAssembly;
        public SerializedType targetType;
        [Inline(true), StringOptionsContext("ComponentProperties")]
        public List<ParameterObject> properties;

        public override void ApplyTo(Component target)
        {
            if (activeSelf.TryGetValue(out bool active))
                target.gameObject.SetActive(active);
            if ($"{targetType} is unassigned.".printWarningIf(targetType.type == null) || $"{targetType.type} not matched {target.GetType()}".printWarningIf(!targetType.type.IsInstanceOfType(target)))
                return;

            foreach (var prop in properties)
                prop.ApplyTo(target);
        }
    }
}
