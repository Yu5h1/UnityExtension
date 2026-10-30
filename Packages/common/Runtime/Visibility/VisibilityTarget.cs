using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    /// <summary>
    /// A scene object whose visibility <see cref="VisibilityTracker"/>s report. <see cref="visible"/> stays true while
    /// any tracker sees it, so two cameras that disagree do not toggle it; <c>visibleChanged</c> carries it to the
    /// scene, for example to start loading the object's content the first time it shows.
    /// </summary>
    public class VisibilityTarget : MonoBehaviour
    {
        internal static readonly List<VisibilityTarget> all = new List<VisibilityTarget>();

        [SerializeField] private UnityEvent<bool> _visibleChanged = new UnityEvent<bool>();

        private readonly HashSet<VisibilityTracker> _seenBy = new HashSet<VisibilityTracker>();
        private Renderer[] _renderers;

        public UnityEvent<bool> visibleChanged => _visibleChanged;
        public bool visible => _seenBy.Count > 0;

        /// <summary>The renderers under this object, found once; for tests that need bounds.</summary>
        public Renderer[] renderers => _renderers ??= GetComponentsInChildren<Renderer>(true);

        private void OnEnable() => all.Add(this);

        private void OnDisable() => all.Remove(this);

        internal void Seen(VisibilityTracker tracker)
        {
            if (_seenBy.Add(tracker) && _seenBy.Count == 1)
                _visibleChanged.Invoke(true);
        }

        internal void Unseen(VisibilityTracker tracker)
        {
            if (_seenBy.Remove(tracker) && _seenBy.Count == 0)
                _visibleChanged.Invoke(false);
        }
    }
}
