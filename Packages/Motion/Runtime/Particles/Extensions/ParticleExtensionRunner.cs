using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Calls a world's registered extensions at the points of a step that ParticleExtensions
    ///   EXT-02 names, in registration order within each stage. An extension that throws is
    ///   logged and the step continues, so one faulty extension cannot leave the solver halfway
    ///   through a substep.
    /// </summary>
    internal sealed class ParticleExtensionRunner
    {
        readonly List<IParticleExtension> _all = new List<IParticleExtension>();
        readonly List<IParticleExtension> _substep = new List<IParticleExtension>();
        readonly List<IParticleExtension> _postStep = new List<IParticleExtension>();

        public ParticleExtensionRunner(ParticleWorld world)
        {
            Context = new ParticleExtensionContext(world);
        }

        public ParticleExtensionContext Context { get; }

        public IReadOnlyList<IParticleExtension> Registered => _all;

        public bool Contains(IParticleExtension extension) => _all.Contains(extension);

        public void Add(IParticleExtension extension)
        {
            if (_all.Contains(extension))
                return;
            _all.Add(extension);
            (extension.Stage == ParticleExtensionStage.Substep ? _substep : _postStep).Add(extension);
        }

        public void Remove(IParticleExtension extension)
        {
            if (!_all.Remove(extension))
                return;
            _substep.Remove(extension);
            _postStep.Remove(extension);
        }

        public void Clear()
        {
            _all.Clear();
            _substep.Clear();
            _postStep.Clear();
        }

        public void Prepare(IParticleDataBinder binder, int step, float deltaTime, ParticleSolverSettings settings)
        {
            ParticleExtensionContext context = Context;
            context.Binder = binder;
            context.Step = step;
            context.StepTime = deltaTime;
            context.SubstepCount = settings.substeps;
            context.Substep = deltaTime / settings.substeps;
            context.SubstepIndex = 0;
            context.Gravity = settings.gravity;
            foreach (IParticleExtension extension in _all)
                Invoke(extension, prepare: true);
        }

        public void RunSubstep(int index)
        {
            if (_substep.Count == 0)
                return;
            Context.SubstepIndex = index;
            foreach (IParticleExtension extension in _substep)
                Invoke(extension, prepare: false);
        }

        public void RunPostStep()
        {
            if (_postStep.Count == 0)
                return;
            Context.SubstepIndex = Context.SubstepCount - 1;
            foreach (IParticleExtension extension in _postStep)
                Invoke(extension, prepare: false);
        }

        void Invoke(IParticleExtension extension, bool prepare)
        {
            try
            {
                if (prepare)
                    extension.Prepare(Context);
                else
                    extension.Execute(Context);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, extension as UnityEngine.Object);
            }
        }
    }
}
