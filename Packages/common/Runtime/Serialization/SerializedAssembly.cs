using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Yu5h1Lib
{
    [Serializable]
    public class SerializedAssembly
    {
        [SerializeField, AutoFill("Assemblies")] private string _assemblyName;

        private Assembly _cached;
        public Assembly assembly
        {
            get
            {
                if (_cached == null && !string.IsNullOrEmpty(_assemblyName))
                    _cached = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == _assemblyName);
                return _cached;
            }
        }

        public string name => _assemblyName;
    }
}