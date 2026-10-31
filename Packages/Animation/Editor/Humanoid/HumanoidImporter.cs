using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Generates the humanoid Avatar and the humanoid clips described by a <see cref="HumanoidImportSettings"/>.
    ///   A source that has a skinned mesh, or no clip at all, is a character: it yields the Avatar. Every clip in the
    ///   source yields one humanoid clip with root motion. Outputs are sub-assets of the settings asset; an output
    ///   that already exists is overwritten in place, so references to it survive, and outputs the source no longer
    ///   produces are removed. A clip whose source animation carries control intervals (see
    ///   <see cref="GlbIntervalReader"/>) also yields a <see cref="MotionIntervals"/> sub-asset; a source without
    ///   intervals yields none and imports as before. Nothing in glTFast or the source file is touched.
    /// </summary>
    public static class HumanoidImporter
    {
        public sealed class Report
        {
            public bool success;
            public string error;
            public Avatar avatar;
            public int mappedBones;
            public readonly List<string> missingJoints = new List<string>();
            public readonly List<ClipReport> clips = new List<ClipReport>();
        }

        public sealed class ClipReport
        {
            public AnimationClip clip;
            public int frames;
            public Vector3 motionEnd;
            public float motionYawEnd;
            public MotionIntervals intervals;
        }

        public static Report Generate(HumanoidImportSettings settings)
        {
            var report = new Report();
            try
            {
                Run(settings, report);
                report.success = true;
            }
            catch (Exception exception)
            {
                report.error = exception.Message;
                Debug.LogException(exception, settings);
            }
            return report;
        }

        static void Run(HumanoidImportSettings settings, Report report)
        {
            var settingsPath = AssetDatabase.GetAssetPath(settings);
            if (string.IsNullOrEmpty(settingsPath))
                throw new InvalidOperationException("The settings must be saved as an asset first.");
            if (settings.source == null)
                throw new InvalidOperationException("Assign the GLB to import.");
            var sourcePath = AssetDatabase.GetAssetPath(settings.source);
            var sourceClips = AssetDatabase.LoadAllAssetsAtPath(sourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToList();

            var map = HumanoidBoneMap.Load();
            HumanoidAvatarBuilder.Result built;
            var restInstance = InstantiateSource(settings);
            try
            {
                built = HumanoidAvatarBuilder.Build(restInstance, map);
                report.mappedBones = built.mappedBones;
                report.missingJoints.AddRange(built.missingJoints);
                if (!built.IsHuman)
                {
                    var mapped = built.mappedBones;
                    Object.DestroyImmediate(built.avatar);
                    throw new InvalidOperationException(
                        $"'{settings.source.name}' does not make a valid humanoid Avatar ({mapped} bones mapped); the required bones are hips, spine, head, arms and legs.");
                }
            }
            finally
            {
                Object.DestroyImmediate(restInstance);
            }

            var isCharacter = sourceClips.Count == 0 || settings.source.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
            var previous = settings.clips.Where(clip => clip != null).ToList();
            var produced = new List<AnimationClip>();
            var previousIntervals = settings.intervals.Where(item => item != null).ToList();
            var producedIntervals = new List<MotionIntervals>();
            var sourceIntervals = ReadIntervals(sourcePath);
            try
            {
                foreach (var sourceClip in sourceClips)
                {
                    var instance = InstantiateSource(settings);
                    HumanoidClipBuilder.Result clipResult;
                    try
                    {
                        clipResult = HumanoidClipBuilder.Build(instance, built.avatar, sourceClip, settings);
                    }
                    finally
                    {
                        Object.DestroyImmediate(instance);
                    }
                    var stored = Upsert(settings, previous.Find(clip => clip.name == clipResult.clip.name), clipResult.clip);
                    produced.Add(stored);
                    var storedIntervals = UpsertIntervals(settings, previousIntervals, sourceIntervals, stored, sourceClips.Count);
                    if (storedIntervals != null)
                        producedIntervals.Add(storedIntervals);
                    report.clips.Add(new ClipReport
                    {
                        clip = stored,
                        frames = clipResult.frames,
                        motionEnd = clipResult.motionEnd,
                        motionYawEnd = clipResult.motionYawEnd,
                        intervals = storedIntervals
                    });
                }
            }
            catch
            {
                Object.DestroyImmediate(built.avatar);
                throw;
            }

            if (isCharacter)
            {
                settings.avatar = Upsert(settings, settings.avatar, built.avatar);
                report.avatar = settings.avatar;
            }
            else
            {
                Object.DestroyImmediate(built.avatar);
                if (settings.avatar != null)
                    Object.DestroyImmediate(settings.avatar, true);
                settings.avatar = null;
            }

            foreach (var stale in previous.Where(clip => !produced.Contains(clip)))
                Object.DestroyImmediate(stale, true);
            settings.clips = produced;

            foreach (var stale in previousIntervals.Where(item => !producedIntervals.Contains(item)))
                Object.DestroyImmediate(stale, true);
            settings.intervals = producedIntervals;

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(settingsPath, ImportAssetOptions.ForceUpdate);
        }

        static List<GlbIntervalReader.Animation> ReadIntervals(string sourcePath)
        {
            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (extension != ".glb" && extension != ".gltf")
                return new List<GlbIntervalReader.Animation>();
            return GlbIntervalReader.Read(File.ReadAllBytes(Path.GetFullPath(sourcePath)));
        }

        static MotionIntervals UpsertIntervals(HumanoidImportSettings settings, List<MotionIntervals> previous,
            List<GlbIntervalReader.Animation> source, AnimationClip clip, int clipCount)
        {
            var animation = source.Find(item => item.name == clip.name);
            if (animation == null && clipCount == 1 && source.Count == 1)
                animation = source[0];
            if (animation == null)
                return null;
            var fresh = ScriptableObject.CreateInstance<MotionIntervals>();
            fresh.name = clip.name + "Intervals";
            fresh.Set(clip, animation.frameRate, animation.length, animation.duration, animation.intervals);
            return Upsert(settings, previous.Find(item => item.name == fresh.name), fresh);
        }

        static GameObject InstantiateSource(HumanoidImportSettings settings)
        {
            var instance = Object.Instantiate(settings.source);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.name = settings.source.name;
            return instance;
        }

        static T Upsert<T>(HumanoidImportSettings owner, T existing, T fresh) where T : Object
        {
            if (existing == null)
            {
                AssetDatabase.AddObjectToAsset(fresh, owner);
                return fresh;
            }
            var name = fresh.name;
            EditorUtility.CopySerialized(fresh, existing);
            existing.name = name;
            Object.DestroyImmediate(fresh);
            return existing;
        }
    }
}
