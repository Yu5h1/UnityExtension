using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib.UI;

public class ButtonAddon : UIControl<Button>
{

	public void InvokeOnClick()
	{
		ui.onClick?.Invoke();
	}
}
