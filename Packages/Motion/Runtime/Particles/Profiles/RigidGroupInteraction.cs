namespace Yu5h1Lib
{
    /// <summary>
    ///   Keeps every particle of the entity in one rigid group that holds its rest shape
    ///   (ParticleSolver SOLVER-15), for fragments and other solid bodies of any layout.
    /// </summary>
    public sealed class RigidGroupInteraction : PhysicsParticleInteraction
    {
        public override bool Accepts(ParticleLayoutKind kind) => true;

        public override void Build(PhysicsParticleLayout layout, int variant, ParticleEntityRequest request)
        {
            int count = layout.GetParticleCount(variant);
            var members = new int[count];
            for (int i = 0; i < count; i++)
                members[i] = i;
            request.rigidGroups.Add(members);
        }
    }
}
