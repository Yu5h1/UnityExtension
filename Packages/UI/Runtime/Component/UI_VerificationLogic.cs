using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.UI
{
	public class UI_VerificationLogic : ScriptableObject
	{
        [SerializeField] private string[] _placeholders;
        public string[] placeholders => _placeholders;
        [SerializeField] private int _codeLength;
        public int codeLength => _codeLength;
        public IEnumerator waitResult { get; set; }

        public virtual void Shown(UI_Verification panel) { }
        public virtual IEnumerator BuildRoutine() => null;
        public virtual UI_Verification.Result GetResult(UI_Verification panel) => new UI_Verification.Result();
    }; 
}