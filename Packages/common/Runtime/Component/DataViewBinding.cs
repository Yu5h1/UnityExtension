using System.Linq;
using UnityEngine;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public class DataViewBinding : ValuePort
    {
        [SerializeField,TypeRestriction(typeof(DataView.Provider))] private Object target;  
        [SerializeField, AutoFill("DataView")] private string key;

        private DataView.Provider _provider;

        protected override void OnInitializing()
        {
            _provider = target as DataView.Provider;
        }

        public override string GetFieldName()
            => string.IsNullOrEmpty(key) ? base.GetFieldName() : key;

        public override string GetValue()
         => _provider?.DataView.TryGetValue(key, out var value) == true ? value : string.Empty;

        public override void SetValue(string value, System.StringComparison comparison)
            => _provider.DataView[key, comparison] = value;

#if  UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        public static void RegisterAutoFill()
        {
            StringOptionsProvider.Register("~DataView", target =>
            {
                if (target == null)
                    return null;
                var binding = target as DataViewBinding;
                if (!(binding.target is DataView.Provider provider))
                    return null;
                if (provider.DataView == null)
                    return null;
                return provider.DataView.Keys.ToArray();
            });
        }
  
#endif

    }
}
