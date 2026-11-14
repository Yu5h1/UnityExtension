using System.Collections.Generic;
using UnityEditor;
using Yu5h1Lib;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Default PhysicsParticle inspector plus its profile compatibility check, shown as error,
    ///   warning and info boxes while editing so an incompatible setup is visible before Play.
    /// </summary>
    [CustomEditor(typeof(PhysicsParticle))]
    public sealed class PhysicsParticleEditor : Editor<PhysicsParticle>
    {
        readonly List<PhysicsParticleMessage> _messages = new List<PhysicsParticleMessage>();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            _messages.Clear();
            targetObject.Validate(_messages);
            foreach (PhysicsParticleMessage message in _messages)
                EditorGUILayout.HelpBox(message.text, ToMessageType(message.severity));
            if (targetObject.LastError != null)
                EditorGUILayout.HelpBox(targetObject.LastError, MessageType.Error);
        }

        static MessageType ToMessageType(PhysicsParticleMessageSeverity severity)
        {
            switch (severity)
            {
                case PhysicsParticleMessageSeverity.Error: return MessageType.Error;
                case PhysicsParticleMessageSeverity.Warning: return MessageType.Warning;
                default: return MessageType.Info;
            }
        }
    }
}
