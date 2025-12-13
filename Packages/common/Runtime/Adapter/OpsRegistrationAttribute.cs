using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Yu5h1Lib
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class OpsRegistrationAttribute : PreserveAttribute
    {
        public Type ComponentType { get; }
        public Type OpsInterface { get; }

        public OpsRegistrationAttribute(Type componentType, Type opsInterface)
        {
            ComponentType = componentType;
            OpsInterface = opsInterface;
        }
    }
}
