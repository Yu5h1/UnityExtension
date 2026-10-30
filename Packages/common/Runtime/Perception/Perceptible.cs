using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    /// <summary>
    /// A scene object that <see cref="Perceiver"/>s look for. <see cref="seen"/> is true while at least one perceiver
    /// sees it, so two cameras that disagree do not toggle it; <c>seenChanged</c> carries that to the scene, for example
    /// to GameObject.SetActive or to the method that starts loading the object's content.
    /// </summary>
    public class Perceptible : MonoBehaviour, IPerceptible
    {
        internal static readonly List<Perceptible> all = new List<Perceptible>();

        [SerializeField] private UnityEvent<bool> _seenChanged = new UnityEvent<bool>();

        private readonly HashSet<IPerceiver> _by = new HashSet<IPerceiver>();

        public event Action<IPerceiver> OnSpotted;
        public event Action<IPerceiver> OnLostSight;

        public UnityEvent<bool> seenChanged => _seenChanged;
        public bool seen => _by.Count > 0;

        private void OnEnable() => all.Add(this);

        private void OnDisable() => all.Remove(this);

        public void Spotted(IPerceiver perceiver)
        {
            if (!_by.Add(perceiver))
                return;
            OnSpotted?.Invoke(perceiver);
            if (_by.Count == 1)
                _seenChanged.Invoke(true);
        }

        public void LostSight(IPerceiver perceiver)
        {
            if (!_by.Remove(perceiver))
                return;
            OnLostSight?.Invoke(perceiver);
            if (_by.Count == 0)
                _seenChanged.Invoke(false);
        }
    }
}
