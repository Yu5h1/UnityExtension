using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class ToggleEx
	{
        public static bool TrySetValueWithoutNotify(this Toggle toggle, bool value)
        {
            if (toggle == null) return false;
            toggle.SetIsOnWithoutNotify(value);
            return true;
        }
    }

}