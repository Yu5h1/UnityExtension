using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.EditorExtension;

[CustomEditor(typeof(UI_Dialog))]
public class UI_DialogEditor : Editor<UI_Dialog>
{
    public override void OnInspectorGUI()
    {
        //serializedTarget.Update();
        base.OnInspectorGUI();
        if (GUILayout.Button("Add element from Content"))
        {
            targetObject.AddElementFromContent();
            EditorUtility.SetDirty(target);
        }
        if (GUILayout.Button("Check & Add Font Text"))
        {
            var charactersFilePath = Path.Combine(Application.dataPath, "Resources", "Font", "Characters.txt");
            if (File.Exists(charactersFilePath))
            {
                var content = File.ReadAllText(charactersFilePath);
                var stringBuilder = new StringBuilder(content);

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
                    }
                }

                var sortedBuilder = new StringBuilder();
                foreach (var c in content.OrderBy(c => (int)c).Reverse())
                    sortedBuilder.Append(c);

                File.WriteAllText(charactersFilePath, sortedBuilder.ToString());

            }
        }
    }
}
