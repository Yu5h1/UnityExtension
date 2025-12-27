using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib.Common;

namespace Yu5h1Lib.UI
{
	public class SelectableOps<T> : OpsBase<T>, ISelectableOps where T : Selectable
	{
		protected SelectableOps(T component) : base(component) { }

        public bool interactable { get => c.interactable; set => c.interactable = value; }
    }
}