using System;
using UnityEngine;

namespace Yu5h1Lib
{
    [Icon("d_Preset.Context")]
    public class ComponentPresetObject : ParameterObject<ComponentPreset> { }

    [Serializable]
    public class ComponentPreset : Preset<Component>
    {
        [Tooltip("Override the GameObject active state.")]
        public Optional<bool> activeSelf;
        public override bool ApplyTo(Component component)
        {
            if (!component)
                return false;
            if (activeSelf.TryGetValue(out bool active))
                component.gameObject.SetActive(active);
            return true;
        }
    }
    public abstract class ComponentPreset<T> : ComponentPreset where T : Component
    {
        public sealed override bool ApplyTo(Component component)
        {
            if ($"Apply to component({typeof(T)}) failed!".printWarningIf(!base.ApplyTo(component)))
                return false;
            if (!(component is T t))
            {
                $"{component.GetType()} {typeof(T)}Type not match".printWarning();
                return false;
            }
            return ApplyTo(t);
        }
        public abstract bool ApplyTo(T component);
    }
}
