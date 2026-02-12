using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Yu5h1Lib
{
    public class Theme : ScriptableObject
    {
        public abstract class Binding : ScriptableObject 
        {
            public abstract void Apply(Theme profile);
        }
        /// <summary>
        /// 主題目標群組基類
        /// key 對應 ThemeProfile 中 ParameterObject 的 name
        /// </summary>
        public abstract class Binding<TComponent, TValue> : Binding
        {
            public string key => name;
            [SerializeField] private TComponent[] _targets;
            public TComponent[] targets => _targets;

            protected abstract void SetValue(TComponent c, TValue value);
            public override void Apply(Theme profile)
            {
                if (!profile.TryGet<TValue>(key, out var value))
                    return;
                foreach (var target in _targets)
                {
                    if (target != null)
                        SetValue(target, value);
                }
            }
        }
        public abstract class BindingPreset<TComponent, TPreset> : Binding<TComponent,TPreset>
            where TComponent : Component
            where TPreset : ComponentPreset
        { protected override void SetValue(TComponent c, TPreset preset) => preset.ApplyToComponent(c); }

        [SerializeField,NotSelf] private Theme _schema;
        public Theme schema => _schema;
        [SerializeField, Inline(true)] private List<ParameterObject> _items;
        public List<ParameterObject> items => _items;

        public bool TryGet<T>(string key, out T value) 
        {

            value = default;
            if (_items == null) return false;

            foreach (var item in _items)
            {
                if (item != null && item.name == key && item is ParameterObject<T> param)
                {
                    value = param.value;
                    return true;
                }
            }

            if (_schema?.TryGet(key, out value) == true)
                return true;

            return false;
        }
 
    }
}
