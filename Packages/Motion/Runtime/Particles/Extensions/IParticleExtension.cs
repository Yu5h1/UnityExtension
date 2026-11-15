using System;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Where in a step an extension runs (ParticleWorld WORLD-13, ParticleExtensions EXT-02).
    /// </summary>
    public enum ParticleExtensionStage
    {
        /// <summary>
        ///   Every substep, after collision shapes and before rigid groups.
        /// </summary>
        Substep,

        /// <summary>
        ///   Once after all substeps, after media.
        /// </summary>
        PostStep
    }

    /// <summary>
    ///   Particle data an extension writes, declared at registration (ParticleExtensions EXT-04).
    /// </summary>
    [Flags]
    public enum ParticleExtensionWrites
    {
        None = 0,
        Positions = 1,
        Velocities = 2
    }

    /// <summary>
    ///   Behavior that runs inside a <see cref="ParticleWorld"/>'s step, registered with
    ///   <see cref="ParticleWorld.RegisterExtension"/> (ParticleExtensions 1.0.0). The world calls
    ///   <see cref="Prepare"/> once per simulated step before the first substep, then
    ///   <see cref="Execute"/> every substep or once after the step, depending on
    ///   <see cref="Stage"/>. Both are skipped when no particle is simulated.
    /// </summary>
    public interface IParticleExtension
    {
        ParticleExtensionStage Stage { get; }

        ParticleExtensionWrites Writes { get; }

        /// <summary>
        ///   Uploads this step's inputs, such as Transform poses.
        /// </summary>
        void Prepare(ParticleExtensionContext context);

        /// <summary>
        ///   Dispatches the extension's kernels. <see cref="ParticleExtensionContext.SubstepIndex"/>
        ///   names the substep for <see cref="ParticleExtensionStage.Substep"/> extensions.
        /// </summary>
        void Execute(ParticleExtensionContext context);
    }
}
