
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    [Icon("d_Preset.Context")]
    public class GenericPresetObject : ParameterObject<GenericObjectPreset> { }

    [System.Serializable]
    public class GenericObjectPreset : Preset<Object>
    {
        public SerializedAssembly targetAssembly;
        public SerializedType targetType;
        [Inline(true), StringOptionsContext("Properties")]
        public List<ParameterObject> properties;

        public override bool ApplyTo(Object obj)
        {
            if ($"{obj.name} {targetAssembly.name}. {targetType} is unassigned.".printWarningIf(targetType.type == null) ||
                $"{targetType.type} not matched {obj.GetType()}".printWarningIf(!targetType.type.IsInstanceOfType(obj)))
                return false;

            foreach (var prop in properties)
                prop.ApplyTo(obj);

            return true;
        }
    }
}
