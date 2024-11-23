using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.EditorExtension
{
    public static class AnimationUtilityEX
    {
        public static AnimationEvent[] GetClipEvents(float start,float end,List<AnimationEvent> XMLevents) {
            List<AnimationEvent> results = new List<AnimationEvent>();
            foreach (var XMLe in XMLevents)
            {
                if (start <= XMLe.time && end >= XMLe.time) {
                    results.Add(new AnimationEvent()
                    {                        
                        functionName = XMLe.functionName,
                        intParameter = XMLe.intParameter,
                        floatParameter = XMLe.floatParameter,
                        stringParameter = XMLe.stringParameter,
                        objectReferenceParameter = XMLe.objectReferenceParameter,
                        time = (XMLe.time - start) / (end - start)
                    });
                }
            }
            return results.ToArray();
        }
        public static List<ModelImporterClipAnimation> LoadTakesFromXML(string xmlPath)
        {
            List<ModelImporterClipAnimation> results = new List<ModelImporterClipAnimation>();
            if (File.Exists(xmlPath))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlPath);
                var XMLevents = LoadXMLEvents(xmlPath);
                foreach (XmlElement element in xmlDoc.DocumentElement["Clips"].ChildNodes)
                {
                    float start = element.GetFloatAttribute("start");
                    float end = element.GetFloatAttribute("end");
                    var newClip = new ModelImporterClipAnimation()
                    {
                        name = element.Name,
                        takeName = element.Name,
                        firstFrame = start,
                        lastFrame = end,
                        events = GetClipEvents(start, end, XMLevents)
                    };
                    results.Add(newClip);
                }
            }
            return results;
        }
        public static List<AnimationEvent> LoadXMLEvents(string xmlPath)
        {
            List<AnimationEvent> results = new List<AnimationEvent>();
            if (File.Exists(xmlPath))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlPath);
                foreach (XmlElement ele in xmlDoc.DocumentElement["Events"].ChildNodes)
                {
                    results.Add(new AnimationEvent() {
                        functionName = ele.Name,
                        time = ele.GetFloatAttribute("frame"),
                        intParameter = ele.GetIntegerAttribute("Int"),
                        floatParameter = ele.GetFloatAttribute("Float"),
                        stringParameter = ele.GetAttribute("String"),                        
                    });
                }
            }
            else { Debug.Log("The XML does not exist which path is "+xmlPath); }
            return results;
        }
        public static List<AnimationEvent> LoadXMLEvents(Object target)
        { return LoadXMLEvents( Path.ChangeExtension(AssetDatabase.GetAssetPath(target), "xml"));}
        public static void UpdateClipsEvents(ModelImporter target,List<AnimationEvent> XMLEvents)
        {            
            SerializedObject serializedObject = new SerializedObject(target);
            var clips = serializedObject.FindProperty("m_ClipAnimations");

            for (int i = 0; i < target.clipAnimations.Length; i++)
            {
                var curClip = target.clipAnimations[i];                
                clips.GetArrayElementAtIndex(i).FindPropertyRelative("events").SetValue(GetClipEvents(curClip.firstFrame, curClip.lastFrame, XMLEvents));
            }
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.WriteImportSettingsIfDirty(target.assetPath);
        }
        [MenuItem("Assets/Import/Load TakeClips from XML")]
        public static void LoadTakesFromXMLPicker()
        {
            string XMLPath = EditorUtility.OpenFilePanel("Select XML", SelectionEx.SelectedAssetPath, "xml");
            if (File.Exists(XMLPath))
            {
                string targetPath = SelectionEx.SelectedAssetPath;
                if (Path.GetExtension(targetPath).ToLower() == ".fbx")
                {
                    var target = AssetImporter.GetAtPath(targetPath) as ModelImporter;
                    var clips = LoadTakesFromXML(XMLPath).ToArray();
                    target.clipAnimations = clips;
                    AssetDatabase.WriteImportSettingsIfDirty(target.assetPath);
                }
                else {
                    EditorUtility.DisplayDialog("Wrong Type", "Please Select a FBX file","ok");
                }
                
            }
            
        }
        [MenuItem("Assets/Import/Load AnimationEvents from XML")]
        public static void LoadAnimationEventsFromXMLPicker()
        {
            string XMLPath = EditorUtility.OpenFilePanel("Select XML", SelectionEx.SelectedAssetPath, "xml");
            if (File.Exists(XMLPath)) {
                List<Object> animeObjs = new List<Object>();
                foreach (var obj in Selection.objects)
                {
                    string objPath = AssetDatabase.GetAssetPath(obj);
                    string extension = Path.GetExtension(objPath).ToLower();
                    if (extension == ".anim") { animeObjs.Add(obj); }
                    else if (extension == ".fbx")
                    {
                        animeObjs.Add((AssetImporter.GetAtPath(objPath) as ModelImporter));
                    }
                }
                var XMLEvents = LoadXMLEvents(XMLPath);
                var XMLTakes = LoadTakesFromXML(XMLPath);
                foreach (var obj in animeObjs)
                {
                    if (obj.GetType() == typeof(AnimationClip))
                    {
                        AnimationClip clip = obj as AnimationClip;
                        foreach (var take in XMLTakes)
                        {
                            if (obj.name == take.takeName) {
                                var currentEvents = take.events;
                                foreach (var curEvent in currentEvents)
                                {
                                    curEvent.time = clip.length * curEvent.time;
                                }
                                AnimationUtility.SetAnimationEvents(clip, currentEvents);
                                AssetDatabase.WriteImportSettingsIfDirty(AssetDatabase.GetAssetPath(clip));
                                break;
                            }
                        }
                    }
                    else if (obj.GetType() == typeof(ModelImporter))
                    {
                        UpdateClipsEvents((ModelImporter)obj, XMLEvents);
                    }
                }
                
            }
        }
    }
}
