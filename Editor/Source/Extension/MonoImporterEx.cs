using UnityEngine;
using UnityEditor;


namespace Yu5h1Lib.EditorExtension
{
    public static class MonoImporterEx
    {

        [MenuItem("CONTEXT/MonoImporter/Create GameObject", true)]
        public static bool InstanceSelectedScriptCheck()
            => SelectionEx.IsMonoScriptSubclassOf<MonoBehaviour>();

        [MenuItem("CONTEXT/MonoImporter/Create GameObject")]
        private static void InstanceSelectedScript()
        {
            new GameObject(Selection.activeObject.name, SelectionEx.GetSelectedScriptClass);
        }
        [MenuItem("CONTEXT/MonoImporter/Create ScriptableObject", true)]
        public static bool CreateScriptableObjectBySelectedValidation()
            => SelectionEx.IsMonoScriptSubclassOf<ScriptableObject>();
        [MenuItem("CONTEXT/MonoImporter/Create ScriptableObject")]
        public static void CreateScriptableObjectBySelected()
            => ScriptableObjectUtil.CreateScriptableObject(SelectionEx.GetSelectedScriptClass);

        [MenuItem("CONTEXT/MonoImporter/Create Derivative")]
        public static void CreateDerivative()
        {
            ScriptTemplateUtil.CreateClass("New"+ Selection.activeObject.name, Selection.activeObject.name);
        }


    } 
}