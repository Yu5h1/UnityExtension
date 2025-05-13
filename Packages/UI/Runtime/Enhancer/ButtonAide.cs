using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonAide : UI_Aide<Button>
{

	public void InvokeOnClick()
	{
		ui.onClick?.Invoke();
	}
}
