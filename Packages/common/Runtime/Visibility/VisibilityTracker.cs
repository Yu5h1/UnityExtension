using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Checks the enabled <see cref="VisibilityTarget"/>s each frame with the <see cref="IVisibilityTest"/> on the same
    /// GameObject and tells a target when this tracker starts or stops seeing it. Disabling the tracker ends its sight of
    /// everything it saw.
    /// </summary>
    public class VisibilityTracker : MonoBehaviour
    {
        public enum Scope
        {
            /// <summary>Only targets under this object, such as a scroll view's content under its viewport.</summary>
            Children,
            /// <summary>Every target in the loaded scenes, such as what a camera may see.</summary>
            Scene,
        }

        [SerializeField] private Scope _scope = Scope.Children;

        private IVisibilityTest _test;
        private readonly HashSet<VisibilityTarget> _seen = new HashSet<VisibilityTarget>();
        private readonly List<VisibilityTarget> _lost = new List<VisibilityTarget>();

        public Scope scope { get => _scope; set => _scope = value; }

        private IVisibilityTest test => _test ??= GetComponent<IVisibilityTest>();

        public bool Sees(VisibilityTarget target)
            => test != null && target != null && InScope(target) && test.IsVisible(target);

        private bool InScope(VisibilityTarget target)
            => _scope == Scope.Scene || (target.transform != transform && target.transform.IsChildOf(transform));

        private void LateUpdate()
        {
            // Backwards by index: a target's listener may disable a target, which removes it from the list.
            var targets = VisibilityTarget.all;
            for (var i = targets.Count - 1; i >= 0; i--)
            {
                if (i >= targets.Count)
                    continue;
                var target = targets[i];
                if (Sees(target))
                {
                    if (_seen.Add(target))
                        target.Seen(this);
                }
                else if (_seen.Contains(target))
                    _lost.Add(target);
            }
            foreach (var target in _seen)
                if (target == null || !target.isActiveAndEnabled)
                    _lost.Add(target);
            foreach (var target in _lost)
            {
                _seen.Remove(target);
                if (target != null)
                    target.Unseen(this);
            }
            _lost.Clear();
        }

        private void OnDisable()
        {
            foreach (var target in _seen)
                if (target != null)
                    target.Unseen(this);
            _seen.Clear();
        }
    }
}
