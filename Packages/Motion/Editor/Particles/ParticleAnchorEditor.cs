using System.Collections.Generic;
using UnityEditor;
using Yu5h1Lib;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Default ParticleAnchor inspector plus an error for every point outside its source's
    ///   particles (ParticleExtensions EXT-06).
    /// </summary>
    [CustomEditor(typeof(ParticleAnchor))]
    public sealed class ParticleAnchorEditor : Editor<ParticleAnchor>
    {
        readonly List<PhysicsParticleMessage> _messages = new List<PhysicsParticleMessage>();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            _messages.Clear();
            targetObject.Validate(_messages);
            foreach (PhysicsParticleMessage message in _messages)
                EditorGUILayout.HelpBox(message.text, MessageType.Error);
        }
    }
}
