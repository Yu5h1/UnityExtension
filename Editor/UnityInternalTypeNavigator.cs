using System.Reflection;
using System;
using UnityEditor;
using UnityEngine;
using CallbackFunction = UnityEditor.EditorApplication.CallbackFunction;

namespace Yu5h1Lib.EditorExtension
{
    public static class UnityInternalTypeNavigator
    {
        public static Assembly EditorAssembly { get { return typeof(Editor).Assembly; } }
        public static Type WindowLayout { get { return GetInternalType("WindowLayout"); } }
        public static Type GetInternalType(string name) { return EditorAssembly.GetType("UnityEditor." + name); }
        //public static Type GetInternalType(string name) { return EditorAssembly.GetType("UnityEditorInternal." + name); }

        public static string[] GetAllAssetNames
        {
            get
            {
                return (typeof(EditorGUIUtility).GetMethod("GetEditorAssetBundle",
                        BindingFlags.Static | BindingFlags.NonPublic).
                        Invoke(null, new object[0]) as AssetBundle).GetAllAssetNames();
            }
        }
        //static FieldInfo globalEventHandlerFieldInfo = typeof(EditorApplication).GetField("globalEventHandler", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

        //public static CallbackFunction globalEventHandler
        //{
        //    get { return (CallbackFunction)globalEventHandlerFieldInfo.GetValue(null); }
        //    set
        //    {
        //        CallbackFunction functions = (CallbackFunction)globalEventHandlerFieldInfo.GetValue(null);
        //        functions += value;
        //        globalEventHandlerFieldInfo.SetValue(null, (object)functions);
        //    }
        //}
    } 
}
