using System;
using System.Xml;
using System.IO;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.Runtime;
using Unity.Jobs.LowLevel.Unsafe;

namespace Yu5h1Lib.EditorExtension
{   
    public class Yu5h1LibPreference
    {
        public static string settingPath {
            get {
                return Path.Combine(AssetDatabaseEx.ProjectSettingLocation, "Yu5h1ToolsPreferenceConfiguration.asset");
            }
        }
        static Yu5h1LibPreference instance;
        public static Yu5h1LibPreference current
        {
            get {
                if (instance == null) {
                    if (!File.Exists(settingPath))
                    {
                        new Yu5h1LibPreference().Save();
                    }
                    instance = JsonUtility.FromJson<Yu5h1LibPreference>(File.ReadAllText(settingPath));                    
                }
                return instance;
            } 
        }        
        public AnimationImportSetting animationImportSetting;

        [PreferenceItem("Yu5h1's Prefer")]
        static void PreferencesGUI() {
            using (var scop = new EditorGUI.ChangeCheckScope())
            {
                current.animationImportSetting.OnGUI();
                if (scop.changed) {
                    current.Save();
                }
            }
        }

        void Save()
        {
            File.WriteAllText(settingPath, JsonUtility.ToJson(this, true));
        }
        [Serializable]
        public class AnimationImportSetting
        {
            public bool enabled;

            public bool RemoveExtraTake001Clip = true;

            public bool keepOriginalOrientation = true;
            public bool BakeInToPoseOrientation = true;

            public bool BakeInToPoseY = true;
            public bool keepOriginalPositionY = true;

            public bool BakeInToPoseXZ;  
            public bool keepOriginalPositionXZ = true;


            public void OnGUI()
            {
                if (EditorGUILayout.Foldout(true, "Animation Import Setting"))
                {
                    EditorGUI.indentLevel = 1;
                    enabled = EditorGUILayout.ToggleLeft("Enabled", enabled);
                    RemoveExtraTake001Clip = EditorGUILayout.ToggleLeft("Remove Extra Take001 Clip", RemoveExtraTake001Clip);

                    BakeInToPoseOrientation = EditorGUILayout.ToggleLeft("Bake In To Pose Orientation", BakeInToPoseOrientation);
                    keepOriginalOrientation = EditorGUILayout.ToggleLeft("keep Original (Orientation)", keepOriginalOrientation);


                    BakeInToPoseY = EditorGUILayout.ToggleLeft("Bake into PoseY", BakeInToPoseY);
                    keepOriginalPositionY = EditorGUILayout.ToggleLeft("keep Original Position (Y)", keepOriginalPositionY);

                    BakeInToPoseXZ = EditorGUILayout.ToggleLeft("Bake into Pose (XZ) ", BakeInToPoseXZ);
                    keepOriginalPositionXZ = EditorGUILayout.ToggleLeft("keepOriginalPosition (XZ) ", keepOriginalPositionXZ);

                    JobsUtility.JobWorkerCount = EditorGUILayout.IntField("JobWorkerCount", JobsUtility.JobWorkerCount);
                }
            }
        }

        public class ImportProcess : AssetPostprocessor
        {
            public static Yu5h1LibPreference setting => Yu5h1LibPreference.current;
            void OnPreprocessModel()
            {
                //ModelImporter target = assetImporter as ModelImporter;
            }
            public static void ImportByYu5h1ToolsDefaultSetting(ModelImporter target)
            {
                if (target.animationType == ModelImporterAnimationType.Generic || target.animationType == ModelImporterAnimationType.Human)
                {
                    if (target.animationType == ModelImporterAnimationType.Generic)
                    {
                        if (target.transformPaths.Length > 1)
                        {
                            var targetSerializedObject = new SerializedObject(target);
                            targetSerializedObject.FindProperty("m_HumanDescription.m_RootMotionBoneName").stringValue =
                                target.transformPaths[1];
                            targetSerializedObject.ApplyModifiedProperties();
                        }
                    }
                    ModelImporterClipAnimation[] takes = target.defaultClipAnimations;
                    if (setting.animationImportSetting.RemoveExtraTake001Clip)
                    {
                        if (target.defaultClipAnimations.Length > 1)
                        {
                            takes = new ModelImporterClipAnimation[takes.Length - 1];
                            for (int i = 0; i < takes.Length; i++)
                            {
                                takes[i] = target.defaultClipAnimations[i + 1];
                            }
                        }
                    }
                    for (int i = 0; i < takes.Length; i++)
                    {
                        takes[i].keepOriginalOrientation = setting.animationImportSetting.keepOriginalOrientation;
                        takes[i].lockRootRotation = setting.animationImportSetting.BakeInToPoseOrientation;
                        takes[i].keepOriginalPositionY = setting.animationImportSetting.keepOriginalPositionY;
                        takes[i].lockRootHeightY = setting.animationImportSetting.BakeInToPoseY;
                        takes[i].keepOriginalPositionXZ = setting.animationImportSetting.keepOriginalPositionXZ;
                        takes[i].lockRootPositionXZ = setting.animationImportSetting.BakeInToPoseXZ;
                    }
                    target.clipAnimations = takes;
                    AssetDatabase.ImportAsset(target.assetPath);
                }
            }
        }
    }

}
