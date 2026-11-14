namespace Yu5h1Lib
{
    public enum PhysicsParticleMessageSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    ///   One editor-time finding about a PhysicsParticle setup. Error means the combination cannot
    ///   work; Warning means it runs but is suspicious; Info is guidance.
    /// </summary>
    public readonly struct PhysicsParticleMessage
    {
        public readonly PhysicsParticleMessageSeverity severity;
        public readonly string text;

        public PhysicsParticleMessage(PhysicsParticleMessageSeverity severity, string text)
        {
            this.severity = severity;
            this.text = text;
        }

        public static PhysicsParticleMessage Error(string text) => new PhysicsParticleMessage(PhysicsParticleMessageSeverity.Error, text);
        public static PhysicsParticleMessage Warning(string text) => new PhysicsParticleMessage(PhysicsParticleMessageSeverity.Warning, text);
        public static PhysicsParticleMessage Info(string text) => new PhysicsParticleMessage(PhysicsParticleMessageSeverity.Info, text);

        public override string ToString() => $"{severity}: {text}";
    }
}
