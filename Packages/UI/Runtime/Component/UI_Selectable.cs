using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
	public abstract class UI_Selectable<T> : UIControl<T> where T : Selectable
    {
		public Selectable selectable => ui;
    }
    public abstract class UI_Selectable<T,TValue> : UIControl<T,TValue> where T : Selectable
    { 
    }
}
