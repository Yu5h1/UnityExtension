using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Builds an <see cref="AudioMixer"/> asset with one child group and one exposed Volume
    /// parameter per requested name. <see cref="AudioMixer"/>'s public API has no way to author
    /// groups or exposed parameters, so this goes through the internal
    /// <c>UnityEditor.Audio.AudioMixerController</c> via reflection — the same type the Audio
    /// Mixer window itself edits. Every member is resolved and null-checked before anything is
    /// created, so a Unity version whose internals moved fails closed with a message instead of
    /// leaving a half-built asset.
    /// </summary>
    public static class AudioMixerBuilder
    {
        private const BindingFlags AnyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static bool TryCreate(string assetPath, IEnumerable<string> groupNames, out AudioMixer mixer, out string error)
        {
            mixer = null;
            var names = groupNames?.Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
            if (names.Count == 0)
            {
                error = "No group names given.";
                return false;
            }
            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(assetPath) != null)
            {
                error = $"An asset already exists at {assetPath}.";
                return false;
            }
            if (!TryResolveApi(out var api, out error))
                return false;

            var folder = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
                System.IO.Directory.CreateDirectory(folder);

            object controller = null;
            try
            {
                controller = api.CreateMixerControllerAtPath.Invoke(null, new object[] { assetPath });
                var masterGroup = api.MasterGroup.GetValue(controller);
                foreach (var groupName in names)
                {
                    var group = api.CreateNewGroup.Invoke(controller, new object[] { groupName, false });
                    api.AddChildToParent.Invoke(controller, new[] { group, masterGroup });

                    var guid = api.GetGuidForVolume.Invoke(group, null);
                    var path = api.GroupParameterPathCtor.Invoke(new[] { group, guid });
                    api.AddExposedParameter.Invoke(controller, new[] { path });
                }
                RenameExposedParameters(api, controller, names);

                EditorUtility.SetDirty((UnityEngine.Object)controller);
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                if (controller != null)
                    AssetDatabase.DeleteAsset(assetPath);
                error = $"Failed to build the mixer: {e.Message}";
                return false;
            }

            mixer = controller as AudioMixer;
            if (mixer == null)
            {
                error = "AudioMixerController did not produce an AudioMixer.";
                return false;
            }
            error = null;
            return true;
        }

        // ExposedAudioParameter is a struct: array.GetValue(i) returns a boxed copy, so the renamed
        // field must be written back into the array with SetValue rather than mutated in place.
        private static void RenameExposedParameters(ReflectedApi api, object controller, IReadOnlyList<string> orderedNames)
        {
            var array = (Array)api.ExposedParameters.GetValue(controller);
            var nameField = array.GetType().GetElementType().GetField("name");
            var offset = array.Length - orderedNames.Count;
            for (int i = 0; i < orderedNames.Count; i++)
            {
                var entry = array.GetValue(offset + i);
                nameField.SetValue(entry, orderedNames[i]);
                array.SetValue(entry, offset + i);
            }
            api.ExposedParameters.SetValue(controller, array);
        }

        private sealed class ReflectedApi
        {
            public MethodInfo CreateMixerControllerAtPath;
            public MethodInfo CreateNewGroup;
            public MethodInfo AddChildToParent;
            public MethodInfo AddExposedParameter;
            public MethodInfo GetGuidForVolume;
            public ConstructorInfo GroupParameterPathCtor;
            public PropertyInfo MasterGroup;
            public PropertyInfo ExposedParameters;
        }

        private static bool TryResolveApi(out ReflectedApi api, out string error)
        {
            api = null;
            var editorAssembly = typeof(Editor).Assembly;
            var controllerType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerController");
            var groupType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerGroupController");
            var pathType = editorAssembly.GetType("UnityEditor.Audio.AudioGroupParameterPath");
            if (controllerType == null || groupType == null || pathType == null)
            {
                error = "UnityEditor.Audio.AudioMixerController API was not found in this Unity version.";
                return false;
            }

            var getGuidForVolume = groupType.GetMethod("GetGUIDForVolume", AnyMember);
            var guidType = getGuidForVolume?.ReturnType;

            api = new ReflectedApi
            {
                CreateMixerControllerAtPath = controllerType.GetMethod("CreateMixerControllerAtPath", AnyMember, null, new[] { typeof(string) }, null),
                CreateNewGroup = controllerType.GetMethod("CreateNewGroup", AnyMember, null, new[] { typeof(string), typeof(bool) }, null),
                AddChildToParent = controllerType.GetMethod("AddChildToParent", AnyMember, null, new[] { groupType, groupType }, null),
                AddExposedParameter = controllerType.GetMethod("AddExposedParameter", AnyMember, null, new[] { pathType }, null),
                GetGuidForVolume = getGuidForVolume,
                GroupParameterPathCtor = guidType == null ? null : pathType.GetConstructor(AnyMember, null, new[] { groupType, guidType }, null),
                MasterGroup = controllerType.GetProperty("masterGroup", AnyMember),
                ExposedParameters = controllerType.GetProperty("exposedParameters", AnyMember),
            };

            if (api.CreateMixerControllerAtPath == null || api.CreateNewGroup == null || api.AddChildToParent == null
                || api.AddExposedParameter == null || api.GetGuidForVolume == null || api.GroupParameterPathCtor == null
                || api.MasterGroup == null || api.ExposedParameters == null)
            {
                error = "AudioMixerController's internal API shape changed; cannot build the mixer.";
                api = null;
                return false;
            }
            error = null;
            return true;
        }
    }
}
