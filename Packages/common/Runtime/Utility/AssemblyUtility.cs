using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assemblies;

namespace Yu5h1Lib
{
	public static class AssemblyUtility
	{
        public static IReadOnlyList<Assembly> GetAssemblies()
        {
#if UNITY_6000_4_OR_NEWER
            return CurrentAssemblies.GetLoadedAssemblies();
#else
            return System.AppDomain.CurrentDomain.GetAssemblies();
#endif
        }
        public static string GetLocation(this Assembly assembly)
        {
#if UNITY_6000_4_OR_NEWER
            return assembly.GetLoadedAssemblyPath();
#else
            return assembly?.Location;
#endif
        }
    } 
}
