
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    [Icon("d_Preset.Context")]
    public class GenericPresetObject : ParameterObject<GenericObjectPreset> {

        public override void ApplyTo(Object target)
        {
            value?.ApplyTo(target);
        }
    }

    [System.Serializable]
    public class GenericObjectPreset : Preset<Object>
    {
        [SerializeField, Tooltip("Relative child path from the target GameObject. Leave empty to apply directly to the supplied target.")]
        private string _path;
        public SerializedType targetType;
        [Inline(true), StringOptionsContext("Properties")]
        public List<ParameterObject> properties;

        public override bool ApplyTo(Object obj)
        {
            if ("Apply target is unassigned.".printWarningIf(obj == null))
                return false;

            var type = targetType?.type;
            if ($"{obj.name} target type is unassigned.".printWarningIf(type == null))
                return false;

            var target = ResolveTarget(obj, type);
            if ($"Could not resolve '{_path}' as {type} from {obj.name}.".printWarningIf(target == null) ||
                $"{type} not matched {target.GetType()}".printWarningIf(!type.IsInstanceOfType(target)))
                return false;

            foreach (var prop in properties)
                prop.ApplyTo(target);

            return true;
        }

        private Object ResolveTarget(Object obj, System.Type type)
        {
            if (string.IsNullOrEmpty(_path))
                return obj;

            GameObject root = null;
            if (obj is GameObject gameObject)
                root = gameObject;
            else if (obj is Component component)
                root = component.gameObject;

            if (root == null)
                return obj;

            var child = root.transform.Find(_path);
            if (child == null)
                return null;

            if (type.IsInstanceOfType(child.gameObject))
                return child.gameObject;
            if (type.IsInstanceOfType(child))
                return child;
            if (typeof(Component).IsAssignableFrom(type))
                return child.GetComponent(type);

            return null;
        }
    }
}
