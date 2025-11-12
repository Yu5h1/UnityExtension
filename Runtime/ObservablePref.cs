using System;
using UnityEngine;

namespace Yu5h1Lib
{
    [Serializable]
    public class ObservablePref<T> : ObservableValue<T>
    {
        [SerializeField] private PlayerPrefValue<T> _pref = new PlayerPrefValue<T>();

        public string key
        {
            get => _pref.key;
            set => _pref.key = value;
        }

        public T defaultValue
        {
            get => _pref.defaultValue;
            set => _pref.defaultValue = value;
        }

        public override T GetDefaultValue() => _pref.defaultValue;
        public override T GetValue() => _pref.Value;
        public override void SetValueWithoutNotify(T newValue) => _pref.Value = newValue;

        public void DeleteKey() => _pref.DeleteKey();
    }
}
