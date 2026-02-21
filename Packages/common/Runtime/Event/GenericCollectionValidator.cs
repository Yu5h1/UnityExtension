using System.Reflection;
using UnityEngine;

namespace Yu5h1Lib
{
    public class GenericCollectionValidator : CollectionValidator<Object>
    {
        [StringOptionsContext("Properties")]
        public string propertyName;

        protected override object GetValue(Object target)
        {
            if (target == null || string.IsNullOrEmpty(propertyName))
                return null;

            var prop = target.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance);

            return prop?.GetValue(target);
        }
    }
}
