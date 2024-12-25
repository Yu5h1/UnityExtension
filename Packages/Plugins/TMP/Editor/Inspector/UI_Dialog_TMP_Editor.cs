using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Text;
using System.Linq;
using TMPro;
using Yu5h1Lib.EditorExtension;
using Yu5h1Lib;

[CustomEditor(typeof(UI_Dialog_TMP))]
public class UI_Dialog_TMP_Editor : Editor<UI_Dialog_TMP>
{
    //static char[] InvalidChars = new char[] { '\n', '\r', '\t', '\b', '\f', '\\', '\'', '\"', '\0', '\v' };
    //SerializedObject serializedTarget;
    void OnEnable()
    {
        //serializedTarget = new SerializedObject(target);
    }
    public override void OnInspectorGUI()
    {
        //serializedTarget.Update();
        base.OnInspectorGUI();
        if (GUILayout.Button("Add lines from Content"))
        {
            targetObject.AddLinesFromContent();
            EditorUtility.SetDirty(target);
        }
        if (GUILayout.Button("Check & Add Font Text"))
        {
            var fontAssetLocation = PathInfo.GetDirectory(AssetDatabase.GetAssetPath(targetObject.textMeshProUGUI.font));
            var charactersFilePath = PathInfo.Combine(Application.dataPath, fontAssetLocation, "Characters.txt");
            charactersFilePath = charactersFilePath.Replace(@"Assets\Assets", "Assets");

            if ($"{charactersFilePath} does not exist".printWarningIf(!File.Exists(charactersFilePath)))
                return;
            var content = File.ReadAllText(charactersFilePath);
            var stringBuilder = new StringBuilder(content);

            string charsAdded = "";
            foreach (var c in targetObject.Content)
            {
                //if (InvalidChars.Contains(c))
                //    continue;
                if (!content.Contains(c))
                {
                    var index = (int)c;
                    if (index > content.Length)
                        index = content.Length;
                    stringBuilder.Insert(index, c);
                    content = stringBuilder.ToString();
                    charsAdded += c;
                }
            }

            var sortedBuilder = new StringBuilder();
            foreach (var c in content.OrderBy(c => (int)c).Reverse())
                sortedBuilder.Append(c);

            File.WriteAllText(charactersFilePath, sortedBuilder.ToString());
            if (charsAdded.IsEmpty())
                $"No character added".print();
            else
                $"{charsAdded} was added".print();
            //TMPro_FontAssetCreatorWindow


        }
    }
}


//[ContextMenu("Check&Add Font Text")]
//public void CheckFontText()
//{
//    var so = new UnityEditor.SerializedObject(this);

//    var persistentCalls = so.FindProperty("OnTriggerEnter2DEvent.m_PersistentCalls.m_Calls");
//    for (int i = 0; i < persistentCalls.arraySize; ++i)
//    {
//        var prop = persistentCalls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments.m_StringArgument");
//        if (prop != null)
//            Debug.Log(prop.stringValue);
//    }

//}
