using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Yu5h1Lib
{
    public class Theme : ScriptableObject
    {
        public interface IBinding
        {
            string key { get; }
            void Apply(Theme profile);
        }
        public abstract class BindingObject : ScriptableObject , IBinding
        {
            public abstract string key { get; }
            public abstract void Apply(Theme profile);
        }
        /// <summary>
        /// 主題目標群組基類
        /// key 對應 ThemeProfile 中 ParameterObject 的 name
        /// </summary>
        public abstract class BindingObject<TUnityObject, TValue> : BindingObject
             where TUnityObject : UnityEngine.Object
        {
            public override string key => name;
            [SerializeField,CollectionConstraint(true)] private TUnityObject[] _targets;
            public TUnityObject[] targets => _targets;

            protected abstract void SetValue(TUnityObject c, TValue value);

            public override void Apply(Theme profile)
            {
                if (!profile.TryGet<TValue>(key, out var value))
                {
                    $"key:{key} not found".printWarning();
                    return;
                }
                foreach (var target in _targets)
                {
                    if (target != null)
                        SetValue(target, value);
                }
            }
        }
        public abstract class BindingPresetObject<TUnityObject, TPreset> : BindingObject<TUnityObject, TPreset> where TUnityObject : UnityEngine.Object
            where TPreset : Preset<TUnityObject>
        {
            protected override void SetValue(TUnityObject c, TPreset value)
            {
                if ($"{name} has null target".printWarningIf(c == null))
                    return;
                if (!value.ApplyTo(c))
                    $"{name} apply to {c.name} failed !".printWarning();
            }
        }
        public abstract class BindingComponentPresetObject<TComponent, TPreset> : BindingObject<TComponent,TPreset>
            where TComponent : Component
            where TPreset : ComponentPreset
        { 
            protected override void SetValue(TComponent c, TPreset preset)
                => $"{name} apply to component({typeof(TComponent)}) failed ! ".printWarningIf(!preset.ApplyTo(c));
        }
        

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
                
                if (item != null && item.name == key )
                {
                    if (item is ParameterObject<T> param)
                    {
                        value = param.value;
                        return true;
                    }
                    else if (item is T val)
                    {
                        value = val;
                        return true;
                    }
                    else
                        $"item:{item} is invalid type.".printWarning();
                }
            }

            if (_schema?.TryGet(key, out value) == true)
                return true;

            return false;
        }
 
    }
    [System.Serializable]
    public abstract class Preset<TObject> where TObject : UnityEngine.Object
    {
        public abstract bool ApplyTo(TObject obj);
    }
}
