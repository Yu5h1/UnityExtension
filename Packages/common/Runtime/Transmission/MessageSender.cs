using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib.MVVM;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib.Transmission
{
	public class MessageSender : MonoBehaviour
	{
        [SerializeField] private GameObject _target;
        public GameObject target { get => _target; set => _target = value; }

        public KeyValues<string, List<ArgumentInfo>> _messageGroups = new();

        public bool TrySend(string msg)
        {
            if ($"MessageSender({name}) Target is not set.".printWarningIf(!target))
                return false;
            MessageReceiver receiver = null;
            if ($"MessageSender({name}) Target({target.name}) does not have a MessageReceiver component.".printWarningIf(!target.TryGetComponent(out receiver)))
                return false;
            if (!_messageGroups.TryGetValue(msg, out var args))
                return false;

            return receiver.TryInvoke(msg, args.ToArray());
        }

        public void Send(string msg) => TrySend(msg);

        public void SetTarget(Component c)
        { 
            if (c is IGetter<GameObject> ggobj)
                target = ggobj.Get();
        }
    } 
}
