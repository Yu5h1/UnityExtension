using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ManagedReferenceTypesAttribute : PropertyAttribute
    {
        public Type[] AllowedTypes;
        public ManagedReferenceTypesAttribute(params Type[] allowedTypes)
        {
            AllowedTypes = allowedTypes;
        }
    }
}
