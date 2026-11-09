using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomEditor(typeof(PartAsset))]
    public class PartAssetEditor : Editor<PartAsset>
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Equipment Part", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Metadata", EditorStyles.boldLabel);
            EditorGUILayout.TextField("ID", targetObject.id);
            EditorGUILayout.TextField("Version", targetObject.version);
            EditorGUILayout.TextField("Kind", targetObject.kind);
            EditorGUILayout.TextField("Format", $"{targetObject.format} ({targetObject.formatVersion})");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Attachment", EditorStyles.boldLabel);
            EditorGUILayout.TextField("Target Bone", targetObject.targetBone);
            EditorGUILayout.Vector3Field("Offset Position", targetObject.offsetPosition);
            EditorGUILayout.Vector3Field("Offset Rotation", targetObject.offsetRotation);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Attach Node (GLB)", EditorStyles.boldLabel);
            EditorGUILayout.Vector3Field("Attach Position", targetObject.attachPosition);
            EditorGUILayout.Vector4Field("Attach Rotation", new Vector4(
                targetObject.attachRotation.x,
                targetObject.attachRotation.y,
                targetObject.attachRotation.z,
                targetObject.attachRotation.w));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Model Prefab", targetObject.prefab, typeof(GameObject), false);

            string assetPath = AssetDatabase.GetAssetPath(targetObject);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string dir = Path.GetDirectoryName(assetPath);
                string filename = Path.GetFileNameWithoutExtension(assetPath);
                string partJson = Path.Combine(dir, filename + ".part.json").Replace('\\', '/');

                EditorGUILayout.Space();
                if (File.Exists(partJson))
                {
                    if (GUILayout.Button("Reimport from .part.json"))
                    {
                        PartImporter.Import(partJson);
                    }
                }
            }
        }
    }
}
