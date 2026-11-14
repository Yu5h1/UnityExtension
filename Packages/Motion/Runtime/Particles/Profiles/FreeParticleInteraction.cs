namespace Yu5h1Lib
{
    /// <summary>
    ///   Particles with no constraints or rigid groups, for loose debris, sand-like piles and static
    ///   lattices of pinned particles.
    /// </summary>
    public sealed class FreeParticleInteraction : PhysicsParticleInteraction
    {
        public override bool Accepts(ParticleLayoutKind kind) => true;

        public override void Build(PhysicsParticleLayout layout, int variant, ParticleEntityRequest request)
        {
        }
    }
}
