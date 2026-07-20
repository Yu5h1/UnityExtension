using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.Timeline;
using UnityEditor.Timeline.Actions;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>Editor-side extension methods for Timeline assets / directors.</summary>
    internal static class TimelineEx
    {
        private const double TimeEpsilon = 1e-6;

        /// <summary>All marker times on the asset — marker track plus every output track.</summary>
        public static IEnumerable<double> EnumerateMarkerTimes(this TimelineAsset asset)
        {
            if (asset == null)
                yield break;

            if (asset.markerTrack != null)
                foreach (var m in asset.markerTrack.GetMarkers())
                    yield return m.time;

            foreach (var track in asset.GetOutputTracks())
                foreach (var m in track.GetMarkers())
                    yield return m.time;
        }

        /// <summary>
        /// Nearest marker time strictly after (forward) / before (!forward) <paramref name="from"/>,
        /// or null when none exists in that direction (or the asset is null).
        /// </summary>
        public static double? FindAdjacentMarkerTime(this TimelineAsset asset, double from, bool forward)
        {
            if (asset == null)
                return null;

            double? best = null;
            foreach (double t in asset.EnumerateMarkerTimes())
            {
                if (forward)
                {
                    if (t > from + TimeEpsilon && (best == null || t < best.Value))
                        best = t;
                }
                else
                {
                    if (t < from - TimeEpsilon && (best == null || t > best.Value))
                        best = t;
                }
            }
            return best;
        }

        /// <summary>
        /// Seeks this director to its previous/next marker and refreshes the Timeline window.
        /// Safe to call on null. Returns false (no-op) when null, not bound to a TimelineAsset,
        /// or no marker lies ahead in that direction.
        /// </summary>
        public static bool SeekToAdjacentMarker(this PlayableDirector director, bool forward)
        {
            if (director == null || director.playableAsset is not TimelineAsset asset)
                return false;

            double? time = asset.FindAdjacentMarkerTime(director.time, forward);
            if (time == null)
                return false;

            director.time = time.Value;
            director.Evaluate();
            TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
            return true;
        }
    }

    /// <summary>
    /// Registers previous/next marker as Shortcut commands so they appear in the Shortcuts Manager
    /// (under the "Timeline" category, by id prefix) and can be bound to keys.
    ///
    /// Global shortcuts — Unity's TimelineWindow is internal and can't be a Shortcut context, so we
    /// can't scope them to the window. Each command no-ops unless a Timeline is being inspected
    /// (the extension is null-safe and direction-safe), so binding them won't misfire elsewhere.
    /// No default keys assigned — bind them in Edit > Shortcuts.
    /// </summary>
    public static class TimelineMarkerShortcuts
    {
        [Shortcut("Timeline/Next Marker")]
        private static void NextMarker() => TimelineEditor.inspectedDirector.SeekToAdjacentMarker(forward: true);

        [Shortcut("Timeline/Prev Marker")]
        private static void PreviousMarker() => TimelineEditor.inspectedDirector.SeekToAdjacentMarker(forward: false);
    }

    /// <summary>
    /// Frames the current Timeline selection, or the entire Timeline when nothing is selected.
    /// Uses Timeline's own actions so clips, markers, tracks, and nested timelines keep their
    /// package-defined framing behaviour.
    /// </summary>
    public static class TimelineFrameShortcuts
    {
        private const string ActionsNamespace = "UnityEditor.Timeline.Actions.";

        private static readonly MethodInfo InvokeWithSelectedMethod = FindInvokeWithSelectedMethod();
        private static readonly MethodInfo InvokeMethod = FindInvokeMethod();

        [Shortcut("Timeline/Frame Selection", typeof(TimelineShortcutContext))]
        private static void FrameSelection()
        {
            if (TimelineEditor.inspectedAsset == null)
                return;

            if (!InvokeTimelineAction("FrameSelectedAction"))
                InvokeTimelineAction("FrameAllAction");
        }

        private static bool InvokeTimelineAction(string actionName)
        {
            var assembly = typeof(Invoker).Assembly;
            var actionType = assembly.GetType(ActionsNamespace + actionName)
                ?? assembly.GetType("UnityEditor.Timeline." + actionName);
            if (actionType == null)
                return false;

            try
            {
                object result;
                if (InvokeWithSelectedMethod != null)
                {
                    result = InvokeWithSelectedMethod.MakeGenericMethod(actionType).Invoke(null, null);
                }
                else if (InvokeMethod != null)
                {
                    object context = CreateActionContext(assembly, InvokeMethod.GetParameters()[0].ParameterType);
                    result = InvokeMethod.MakeGenericMethod(actionType).Invoke(null, new[] { context });
                }
                else
                {
                    return false;
                }

                return result is not bool invoked || invoked;
            }
            catch (Exception exception)
            {
                $"Unable to invoke Timeline action {actionName}: {exception.GetBaseException().Message}".printWarning();
                return false;
            }
        }

        private static object CreateActionContext(Assembly assembly, Type contextType)
        {
            object context = Activator.CreateInstance(contextType);
            SetField(contextType, context, "timeline", TimelineEditor.inspectedAsset);
            SetField(contextType, context, "director", TimelineEditor.inspectedDirector);
            contextType.GetProperty("clips")?.SetValue(context, TimelineEditor.selectedClips);

            var selectionManager = assembly.GetType("UnityEditor.Timeline.SelectionManager");
            if (selectionManager != null)
            {
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                object markers = selectionManager.GetMethod("SelectedMarkers", flags)?.Invoke(null, null);
                contextType.GetProperty("markers")?.SetValue(context, markers);

                var selectedItemOfType = selectionManager.GetMethod("SelectedItemOfType", flags);
                object tracks = selectedItemOfType?.MakeGenericMethod(typeof(TrackAsset)).Invoke(null, null);
                contextType.GetProperty("tracks")?.SetValue(context, tracks);
            }

            return context;
        }

        private static void SetField(Type type, object instance, string name, object value)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            type.GetField(name, flags)?.SetValue(instance, value);
        }

        private static MethodInfo FindInvokeWithSelectedMethod()
        {
            foreach (var method in typeof(Invoker).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.Name == "InvokeWithSelected"
                    && method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == 1
                    && method.GetParameters().Length == 0)
                    return method;
            }

            return null;
        }

        private static MethodInfo FindInvokeMethod()
        {
            foreach (var method in typeof(Invoker).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.Name == "Invoke"
                    && method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == 1
                    && method.GetParameters().Length == 1)
                    return method;
            }

            return null;
        }
    }

    /// <summary>Activates Timeline shortcuts only while the Timeline window has focus.</summary>
    [InitializeOnLoad]
    public sealed class TimelineShortcutContext : IShortcutContext
    {
        private const string TimelineWindowTypeName = "UnityEditor.Timeline.TimelineWindow";

        private static readonly TimelineShortcutContext Instance = new();

        static TimelineShortcutContext()
        {
            ShortcutManager.RegisterContext(Instance);
        }

        public bool active
        {
            get
            {
                var window = EditorWindow.focusedWindow;
                return window != null && window.GetType().FullName == TimelineWindowTypeName;
            }
        }
    }
}
