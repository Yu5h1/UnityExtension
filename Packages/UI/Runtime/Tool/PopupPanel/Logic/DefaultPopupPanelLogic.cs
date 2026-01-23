using UnityEngine;

namespace Yu5h1Lib.UI
{
	public class DefaultPopupPanelLogic : PopupPanel.LogicObject
	{

        public override PopupPanel.Result GetResult(object sender)
        {
            return new PopupPanel.Result(){ Succeeded = true,Content = "Test popuplogic"};
        }
	} 
}
