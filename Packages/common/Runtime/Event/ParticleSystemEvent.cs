using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using static UnityEngine.ParticleSystem;

namespace Yu5h1Lib
{
    public class ParticleSystemEvent : ParticleSystemEvent<Collider> { }
    public class ParticleSystemEvent<TCollider> : ParticleSystemControllerBase where TCollider : Component
    {

        [SerializeField]
        private TagLayerMask filter;
        [SerializeField]
        private UnityEvent<TCollider> _triggerEnter;
        [SerializeField]
        private UnityEvent<GameObject> _particleCollision;
        [SerializeField,FormerlySerializedAs("OnParticleSystemStoppedEvent")]
        private UnityEvent _stopped;

        protected ParticleSystem.Particle[] particles;


        public ParticleSystem[] subParticleSystems { get; private set; }


        /// <summary>
        /// Unable to capture message
        /// </summary>
        //[SerializeField]
        //private UnityEvent<ParticleSystem.Particle> _ParticleBirth;
        //[SerializeField]
        //private UnityEvent<ParticleSystem.Particle> _ParticleDeath;


        private void Reset()
        {
            Init();
        }
        private void Start()
        {
            var main = particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
            subParticleSystems = GetComponentsInChildren<ParticleSystem>();
        }
        public void SetTriggerList(IEnumerable<TCollider> targets)
        {
            if (filter.layers.value == 0)
                return;
            particleSystem.SetTriggerList(filter.Filter(targets));
        }

        //private void FixedUpdate()
        //{
        //if (!TriggerByCircleCast)
        //    return;

        //}
        private void OnParticleTrigger()
        {
            if (TryGetTriggerCollider(ParticleSystemTriggerEventType.Enter, out Particle particle, out Component component)
                && component is TCollider collider && filter.Validate(this, collider))
                _triggerEnter?.Invoke(collider);
        }
        private void OnParticleTriggerEnter(GameObject gameObject)
        {

        }

        protected void OnParticleSystemStopped()
        {
            if (!IsAvailable())
                return;
            _stopped?.Invoke();
        }
        private void OnParticleCollision(GameObject other)
        {
            _particleCollision?.Invoke(other);
        }
        //private void OnParticleUpdateJobScheduled() {}
        public void DismissParticleOnTriggerEnter()
        {
            particleSystem.ModifyTriggerParticles(ParticleSystemTriggerEventType.Enter, DismissParticle);
        }
        public void DismissParticles()
        {
            var particles = new Particle[particleSystem.main.maxParticles];
            particleSystem.ModifyParticle(ref particles, DismissParticle);
        }
        public Particle DismissParticle(Particle source)
        {
            source.remainingLifetime = 0.01f;
            return source;
        }
        #region Enhance...
        public bool TryGetTriggerCollider(ParticleSystemTriggerEventType eventType, out Particle particle, out Component component)
        {
            component = null;
            particle = default;
            var particles = new List<Particle>();
            int numOfparticles = particleSystem.GetTriggerParticles(eventType, particles, out ColliderData data);
            for (int p = 0; p < numOfparticles; p++)
            {
                for (int c = 0; c < data.GetColliderCount(p); c++)
                    if (component = data.GetCollider(p, c))
                    {
                        particle = particles[p];
                        return true;
                    }
            }
            return false;
        }
        private void OnDisable()
        {
            particleSystem.ClearTriggerList();
        }
        #endregion
    }
}