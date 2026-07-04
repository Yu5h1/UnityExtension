using UnityEngine;

namespace Yu5h1Lib
{
    public interface IParameter 
    {
        string name { get; }
        void ApplyTo(Object target);
        object GetValue();
    }
}