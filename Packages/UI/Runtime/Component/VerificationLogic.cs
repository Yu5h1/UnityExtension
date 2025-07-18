using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib.UI;

namespace Yu5h1Lib
{
	public class VerificationLogic : ScriptableObject
	{
        public delegate Result ResultHandler(object sender);
        public class Result
        {
            public bool Succeeded;
            public string Content = "Not Implemented ! ";
            public static implicit operator bool(Result r) => r != null && r.Succeeded;

        }

        [SerializeField] private InputFieldSetting[] _inputFieldAdapterSettings;
        public InputFieldSetting[] inputFieldAdapterSettings => _inputFieldAdapterSettings;
        public bool showButtons;

        public IEnumerator waitResult { get; set; }

        //public virtual void Shown(UI_Verification panel) { }
        public virtual IEnumerator BuildRoutine() => null;
        public virtual Result GetResult(object sender) => new Result();
    }; 
}