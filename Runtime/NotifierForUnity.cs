using System;
using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public class NotifierForUnity : Notifier
    {
        public override uint Priority => 100;

        public readonly static Actions DefaultPrints = new Actions()
        {
            Info = (string message, string caption) => Debug.Log(message),
            Warning = (string message, string caption) => Debug.LogWarning(message),
            Error = (string message, string caption) => Debug.LogError(message),
        };

        protected override Actions CreatePrints() => DefaultPrints;
        protected override Actions CreatePopups() => null;
        protected override Func<string, string, int, int, int> CreateQueryBox() => null;

    }
}
