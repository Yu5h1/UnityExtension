using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Yu5h1Lib
{
    public class VolumeController : MonoBehaviour
    {
        [System.Serializable]
        public abstract class Animatable<T> where T : VolumeComponent
        {
            public T Component;
            public abstract void Read();
            public abstract void Write();
        }
        public Volume volume;
        [SerializeField,ReadOnly]
        private VolumeProfile _sourceVolumeProfile;
        private VolumeProfile volumeProfile => volume.profile;

        public Bloom bloom;

        void Start()
        {
            TryGetComponent(out volume);
            _sourceVolumeProfile = volume.profile;
            if (_sourceVolumeProfile)
                volume.profile = Object.Instantiate(_sourceVolumeProfile);
        }

        public void ToggleActivate<T>(bool active) where T : VolumeComponent
        {
            if (volumeProfile.TryGet(out T component))
                component.active = active;
        }
    }
}
