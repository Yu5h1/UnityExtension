using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    [System.ComponentModel.Browsable(false), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ParticleSystemEx
	{
        public static void SetTriggerList<T>(this ParticleSystem particleSystem, IEnumerable<T> targets) where T : Component
        {
            var triggerModule = particleSystem.trigger;
            triggerModule.enabled = true;
            particleSystem.ClearTriggerList();
            foreach (var target in targets)
                triggerModule.AddCollider(target);
        }
        public static Component[] GetTriggerList(this ParticleSystem particleSystem)
        {
            var triggerModule = particleSystem.trigger;
            var results = new Component[triggerModule.colliderCount];
            for (int i = 0; i < results.Length; i++)
                results[i] = triggerModule.GetCollider(i);
            return results;
        }
        public static void ClearTriggerList(this ParticleSystem particleSystem)
        {
            var triggerModule = particleSystem.trigger;
            var targets = particleSystem.GetTriggerList();
            foreach (var target in targets)
                triggerModule.RemoveCollider(target);
        }
    } 
}
