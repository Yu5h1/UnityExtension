using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public class Preferences : SingletonBehaviour<Preferences>
    {
        [SerializeField] private List<Object> bindings;

        [SerializeField] protected DataView defaultSetting;
        public static DataView DefaultSetting() => new DataView(instance.defaultSetting);

        [SerializeField, ReadOnly] private DataView _current = new DataView();
        public DataView current => _current;


        protected override void OnInstantiated() { }
        protected override void OnInitializing()
        {
            if (current.IsEmpty())
                current.CopyFrom(DefaultSetting());

            if (LoadPreferences())
            {
                foreach (var obj in bindings)
                    GetProperty(obj);
            }
        }

        public void GetProperty(Object obj) => current.GetProperty(obj);
        public void SetProperty(Object obj) => current.SetProperty(obj);

        public virtual bool LoadPreferences() => true;
    }
}