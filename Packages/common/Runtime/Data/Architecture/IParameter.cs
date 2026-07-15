using UnityEngine;

namespace Yu5h1Lib
{
    public interface IParameter 
    {
        string memberName { get; }
        void ApplyTo(Object target);
        object GetValue();
    }
}