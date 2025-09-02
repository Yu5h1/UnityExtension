using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;
using Yu5h1Lib;

namespace UnityEngine.UI
{
	[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
	public static class SelectableEx
	{
		public static void Deselect(this Selectable selectable)
		{
			if (EventSystem.current == null)
				return;
			if (EventSystem.current.currentSelectedGameObject != selectable.gameObject)
				return;
#if UNITY_EDITOR
			"EventSystem SetSelectedGameObject(null)".print();
#endif
            EventSystem.current.SetSelectedGameObject(null);
        }
		//public static void SelectNext(this Selectable selectable,Direction2D direction)
		//{

		//}
	} 
}
