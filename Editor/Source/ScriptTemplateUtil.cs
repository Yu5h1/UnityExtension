using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public class ScriptTemplateUtil
    {
        static string BuiltInScriptTemplatesPath
            => Path.GetDirectoryName(EditorApplication.applicationPath) + @"\Data\Resources\ScriptTemplates";
        //=> typeof(ScriptTemplateUtil).Assembly.Location + @"\ScriptTemplates";
        static string GetTemplateFilePath(string fileName, bool UseBuiltInLocation = false)
            => BuiltInScriptTemplatesPath + @"\" + fileName;

        /// <summary>
        /// if not exists in the local folder then load from resources.
        /// </summary>
        static string GetTemplate(string fileName, string content) {
            var filepath = GetTemplateFilePath(fileName);
            if (File.Exists(filepath)) content = File.ReadAllText(filepath);
            return content;
        }
        [MenuItem("Assets/Create/Miscellaneous/Exeplore Built-in Templetes Folder")]
        public static void ExeploreTempletesFolder() { System.Diagnostics.Process.Start(@"" + BuiltInScriptTemplatesPath); }
        static bool DetectScriptTemplateExist(string templateFilePath)
        {
            bool result = File.Exists(templateFilePath);
            if (result == false) Debug.LogWarning(templateFilePath + "\nThe script template which named {" + Path.GetFileName(templateFilePath) + "} is not exist.");
            return result;
        }
        static void CreateScriptAssetWithContents(string scriptFileName,string contents, System.Func<string, string, string> ReplaceContentAction = null, bool PlaceInEditorFolder = false)
        {
            var IconsOfExtensions = new Dictionary<string, string> {
                { ".cs" , "cs Script Icon"},
                { ".shader" , "shader icon"},
                { ".txt" , "textasset icon"}
            };
            if (PlaceInEditorFolder) 
                ProjectBrowserEx.SelectEditorFolder();

            CustomEndNameEditAction.ReplaceContentAction = ReplaceContentAction;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
            ScriptableObject.CreateInstance<CustomEndNameEditAction>(), scriptFileName,
            EditorGUIUtility.IconContent(IconsOfExtensions[Path.GetExtension(scriptFileName)]).image as Texture2D,
            contents);
        }
        [MenuItem("Assets/Create/Editor/Inspector", false, 85)]
        private static void CreateInspector()
        {
            string className = Selection.activeObject.GetType().Name.ToString();
            if (SelectionEx.IsMonoBehaviourSelected) 
                className = Selection.activeObject.name;

            var templateFileName = className + "Editor.cs";

            CreateScriptAssetWithContents(templateFileName,
                GetTemplate(
                    "C# Script-Inspector.cs.txt",
                    Properties.Resources.C__Script_Inspector_cs
                ), 
                (fileName,content) => content.Replace("#SCRIPTNAME#", className)
                , true);
        }
        [MenuItem("Assets/Create/Editor/Editor Window", false, 85)]
        static void CreateFocusSingleInstanceWindow() {
            CreateScriptAssetWithContents("NewEditorWindow.cs",
                GetTemplate(
                    "C# Script-EditorWindow.cs.txt",
                    Properties.Resources.C__Script_EditorWindow_cs
                ),null,true); }

        [MenuItem("Assets/Create/Editor/PropertyDrawer Script", false, 85)]
        static void CreatePropertyDrawerScript()
        {   
            CreateScriptAssetWithContents("NewPropertyDrawer.cs", 
                GetTemplate(
                    "C# Script-PropertyDrawer.cs.txt",
                Properties.Resources.C__Script_PropertyDrawer_cs ) ,
            (filename, content) => content.Replace("#SCRIPTNAME#", filename.Replace("PropertyDrawer", "")), true); 
        }
        [MenuItem("Assets/Create/Miscellaneous/ScriptableObject Script", false, 85)]
        static void CreateScriptableObjectScript() {
            CreateScriptAssetWithContents("NewScriptableObject.cs", 
                GetTemplate(
                    "C# Script-ScriptableObject.cs.txt",
                    Properties.Resources.C__Script_ScriptableObject_cs
                ));
        }

        [MenuItem("Assets/Create/Miscellaneous/Text file", false, 85)]
        static void CreateTextFile()
        {
            CreateScriptAssetWithContents("NewText.txt", "");
        }
        public static void CreateClass(string name,string baseClass) {
            if (baseClass != string.Empty) baseClass = ": " + baseClass;
            CreateScriptAssetWithContents(name+".cs",
            @"using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class #SCRIPTNAME# "+baseClass+@"{
}"
            );
        }

        [MenuItem("Assets/Create/Miscellaneous/Class", false, 85)]
        static void CreateClass()
        {
            CreateClass("NewClass.cs",string.Empty);
        }
        [MenuItem("Assets/Create/Miscellaneous/Script from Clipboard", true, 85)]
        static bool CreateScriptFromClipboardValidation()
        {
            return FindScriptNameFromText(EditorGUIUtility.systemCopyBuffer) != "";
        }
        [MenuItem("Assets/Create/Miscellaneous/Script from Clipboard", false, 85)]
        static void CreateScriptFromClipboard()
        { CreateScriptAssetWithContents(FindScriptNameFromText(EditorGUIUtility.systemCopyBuffer), EditorGUIUtility.systemCopyBuffer); }
        static string FindScriptNameFromText(string txt)
        {
            string fileName = "";
            foreach (var line in txt.Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.None))
            {
                if (line.Contains("class "))
                {
                    var splitWords = line.Split(' ').ToList();
                    fileName = AssetDatabaseEx.RemoveInvalidFileNameChars(splitWords[splitWords.FindIndex(d => d.ToLower() == "class") + 1]) + ".cs";
                    break;
                }
                if (line.Contains("Shader \""))
                {
                    var splitWords = line.Split('"').ToList();
                    fileName = AssetDatabaseEx.RemoveInvalidFileNameChars(Path.GetFileNameWithoutExtension(splitWords[1])) + ".shader";
                    break;
                }
            }
            return fileName;
        }
        [MenuItem("Assets/Recompile Scripts _F5", false,40)]
        public static void ForceReCompileAssembly()
        {
            var g = EditorUserBuildSettings.selectedBuildTargetGroup;
            string s = PlayerSettings.GetScriptingDefineSymbolsForGroup(g);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(g, s+ "ForceRecompile");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(g, s);

        }
    }
}
