using System;
using UnityEngine;

namespace Yu5h1Lib
{
    [Icon("d_Preset.Context")]
    public class ComponentPresetObject : ParameterObject<ComponentPreset> { }

    [Serializable]
    public class ComponentPreset
    {
        [Tooltip("Override the GameObject active state.")]
        public Optional<bool> activeSelf;
        public virtual bool ApplyToComponent(Component component)
        {
            if ("nukk".printWarningIf(!component))
                return false;
            if (activeSelf.TryGetValue(out bool active))
                component.gameObject.SetActive(active);
            return true;
        }
    }
    public abstract class ComponentPreset<T> : ComponentPreset where T : Component
    {
        public sealed override bool ApplyToComponent(Component component)
        {
            if (!base.ApplyToComponent(component))
                return false;
            if (!(component is T t))
            {
                $"{component.GetType()} {typeof(T)}Type not match".printWarning();
                return false;
            }
            $"{typeof(T)}".print();
            return ApplyTo(t);
        }
        public abstract bool ApplyTo(T component);
    }
}
