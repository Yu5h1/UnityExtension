using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Checks every enabled <see cref="Perceptible"/> each frame with the <see cref="ISightStrategy"/> on the same
    /// GameObject and tells a target when this perceiver starts or stops seeing it. Disabling the perceiver ends its
    /// sight of everything it saw.
    /// </summary>
    public class Perceiver : MonoBehaviour, IPerceiver
    {
        private ISightStrategy _strategy;
        private readonly HashSet<Perceptible> _seen = new HashSet<Perceptible>();
        private readonly List<Perceptible> _lost = new List<Perceptible>();

        private void Awake() => _strategy = GetComponent<ISightStrategy>();

        public bool CanSee(IPerceptible target) => _strategy != null && _strategy.Check(this, target);

        private void LateUpdate()
        {
            // Backwards by index: a target's listener may disable a target, which removes it from the list.
            for (var i = Perceptible.all.Count - 1; i >= 0; i--)
            {
                if (i >= Perceptible.all.Count)
                    continue;
                var target = Perceptible.all[i];
                var sees = CanSee(target);
                if (sees && _seen.Add(target))
                    target.Spotted(this);
                else if (!sees && _seen.Contains(target))
                    _lost.Add(target);
            }
            foreach (var target in _seen)
                if (target == null || !target.isActiveAndEnabled)
                    _lost.Add(target);
            foreach (var target in _lost)
            {
                _seen.Remove(target);
                if (target != null)
                    target.LostSight(this);
            }
            _lost.Clear();
        }

        private void OnDisable()
        {
            foreach (var target in _seen)
                if (target != null)
                    target.LostSight(this);
            _seen.Clear();
        }
    }
}
