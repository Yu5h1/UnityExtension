using UnityEngine;
using UnityEngine.Playables;

namespace Yu5h1Lib.Timeline
{
    /// <summary>
    /// Receives a <see cref="ParameterSignal"/> and does two things:
    /// re-emits the carried <see cref="ParameterObject"/> through the base UnityEvent
    /// (designer wires the response), and, if <see cref="_target"/> is set,
    /// writes the ParameterObject onto it via reflection (ApplyTo).
    /// </summary>
    [ExecuteAlways]
    public class ParameterReceiver : SignalReceiver<ParameterSignal, ParameterObject> 
    {
        [SerializeField] private Object _target;

        public override void OnNotify(Playable origin, INotification notification, object context)
        {
            base.OnNotify(origin, notification, context);

            if (notification is not ParameterSignal signal || signal.Value == null || _target == null)
                return;
            signal.Value.ApplyTo(_target);
        }
    }
}
