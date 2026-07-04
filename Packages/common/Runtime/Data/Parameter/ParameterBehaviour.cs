using UnityEngine;

namespace Yu5h1Lib.Parameter
{
    public abstract class ParameterBehaviour : MonoBehaviour, IParameter
    {
        public abstract void ApplyTo(Object target);        
        public abstract object GetValue();
    }
}
