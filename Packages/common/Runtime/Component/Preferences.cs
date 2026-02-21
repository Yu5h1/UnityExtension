using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public class Preferences : SingletonBehaviour<Preferences>
    {
        [SerializeField] private List<Object> _bindings;
        public IReadOnlyList<Object> bindings => _bindings;

        [SerializeField] protected DataView defaultSetting;
        public static DataView DefaultSetting() => new DataView(instance.defaultSetting);

        [SerializeField, ReadOnly] private DataView _current = new DataView();
        public DataView current => _current;


        public bool TryGetValueFromBindings(string key,out string value)
        {
            value = default;
            foreach (var obj in bindings)
            {
                if (obj is IValuePort port && port.GetFieldName() == key)
                {
                    value = port.GetValue();
                    return true;
                }
            }
            $"Key [{key}] not found in bindings.".printWarning();
            return false;
        }

        public string GetValueFromBindings(string key)
        {
            if (TryGetValueFromBindings(key, out string value))
                return value;
            else
            {
                $"Key [{key}] not found in bindings.".printWarning();
                return default;
            }


        }

        protected override void OnInstantiated() { }
        protected override void OnInitializing()
        {
            if (current.IsEmpty())
                current.CopyFrom(DefaultSetting());

            if (IsCacheLoaded())
            {
                foreach (var obj in bindings)
                    WriteTo(obj);
            }
        }


        public void WriteToBindings()
        { 
            foreach (var obj in _bindings)
                WriteTo(obj);
        }
        public void ReadFromBindings()
        {
            foreach (var obj in _bindings)
                ReadFrom(obj);
        }


        public void WriteTo(Object obj) => current.WriteTo(obj);
        public void ReadFrom(Object obj) => current.ReadFrom(obj);

        public void GetProperty(Object obj) => current.WriteTo(obj);
        public void SetProperty(Object obj) => current.ReadFrom(obj);

        /// <summary>
        /// Determines whether the current instance is in a ready state.
        /// </summary>
        /// <returns><see langword="true"/> if the instance is ready; otherwise, <see langword="false"/>.</returns>
        public virtual bool IsCacheLoaded() => true;
    }
}