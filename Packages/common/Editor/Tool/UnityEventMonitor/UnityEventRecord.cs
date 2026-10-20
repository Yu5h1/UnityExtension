// One persistent call found in a scene or asset, plus its validity.
// Produced by UnityEventScanner; read by UnityEventViewer and UnityEventReport.
using System;
using UnityEngine.Events;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Validity of a persistent call, judged with the same rule Unity uses at runtime.
    /// Ordered from most to least severe so sorting by status puts broken calls first.
    /// </summary>
    public enum PersistentCallStatus
    {
        /// <summary>The target reference points at an object that no longer exists or whose script is missing.</summary>
        MissingTarget,
        /// <summary>No method with this name exists on the target type.</summary>
        MethodNotFound,
        /// <summary>The stored argument type name no longer resolves, so Unity looks for an Object parameter and fails.</summary>
        StaleArgumentType,
        /// <summary>A method with this name exists but none accepts the call's argument.</summary>
        SignatureMismatch,
        /// <summary>The target field is empty.</summary>
        NullTarget,
        /// <summary>A target is set but no function is chosen.</summary>
        NoFunction,
        /// <summary>Callable, but the stored target type name no longer resolves; the call cannot be shown if the target is lost.</summary>
        StaleTargetType,
        Valid,
    }

    [Serializable]
    public class UnityEventRecord
    {
        public string assetPath;
        public string ownerPath;
        public string ownerType;
        public string eventPath;
        public int index;
        public string targetPath;
        public string targetType;
        public string methodName;
        public PersistentListenerMode mode;
        public string argument;
        public UnityEventCallState callState;
        public PersistentCallStatus status;
        public string detail;

        [NonSerialized] public UnityEngine.Object owner;
        [NonSerialized] public UnityEngine.Object target;

        public bool IsBroken => status < PersistentCallStatus.StaleTargetType;
    }
}
