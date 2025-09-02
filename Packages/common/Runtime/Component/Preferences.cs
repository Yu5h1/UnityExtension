using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public class Preferences : SingletonBehaviour<Preferences>
    {
        protected override void OnInstantiated() { }

        [SerializeField] protected DataView defaultSetting;
        public static DataView DefaultSetting() => new DataView(instance.defaultSetting);

        [SerializeField, ReadOnly] private DataView _current = new DataView();
        public DataView current => _current;

        protected override void OnInitializing()
        {
            if (current.IsEmpty())
                current.CopyFrom(DefaultSetting());
        }

        public void GetProperty(Object obj) => current.GetProperty(obj);
        public void SetProperty(Object obj) => current.SetProperty(obj);
    }
}