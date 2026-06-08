using UnityEngine;


namespace Yu5h1Lib
{
    [RequireComponent(typeof(ParticleSystem))]
    public abstract class ParticleSystemControllerBase : ComponentController<ParticleSystem>
    {
#pragma warning disable 0109
        public new ParticleSystem particleSystem
        {
            get => component;
            //protected set => component = value; 
        }
#pragma warning restore 0109
        public float normalizedTime => particleSystem.time.GetNormal(particleSystem.main.duration);
    }    
}
