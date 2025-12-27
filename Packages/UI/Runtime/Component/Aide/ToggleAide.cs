using UnityEngine;
using UnityEngine.Events;

namespace UnityEngine.UI
{
	public class ToggleAide : UI_Aide<Toggle>
	{

		public void	ToggleIsOn() => ui.isOn = !ui.isOn;

		[SerializeField] private UnityEvent _checked = new UnityEvent();
		public event UnityAction checkedCallback
		{ 
			add => _checked.AddListener(value);
			remove => _checked.RemoveListener(value);
        }
		[SerializeField] private UnityEvent _unchecked = new UnityEvent();
		public event UnityAction uncheckedCallback
		{ 
			add => _unchecked.AddListener(value);
			remove => _unchecked.RemoveListener(value);
        }
        private void Start()
        {
            ui.onValueChanged.AddListener(OnValueChanged);
        }

		private void OnValueChanged(bool isOn)
		{ 
			if (isOn)
				_checked.Invoke();
			else
				_unchecked.Invoke();
        }

		public void SetCanvasGroupAlpha(CanvasGroup cg) => cg.alpha = ui.isOn ? 1 : 0;

    } 
}
