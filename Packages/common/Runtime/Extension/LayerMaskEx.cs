using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
	[EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
	public static class LayerMaskEx
	{
		public static bool Contains(this LayerMask layerMask, GameObject gameObject)
			 => ((1 << gameObject.layer) & layerMask.value) != 0;
	} 
}