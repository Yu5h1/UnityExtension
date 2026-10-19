using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>
    /// Button demo interaction: each click flips the button's selected state and logs it.
    /// </summary>
    public class UxUiButtonDemoToggle : MonoBehaviour
    {
        [SerializeField] private ButtonAddon _button;
        [SerializeField] private Text _log;

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.ui.onClick.AddListener(Toggle);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.ui.onClick.RemoveListener(Toggle);
            }
        }

        private void Toggle()
        {
            _button.isSelected = !_button.isSelected;
            if (_log != null)
            {
                string state = _button.isSelected ? "Selected" : "Unselected";
                _log.text = $"Last clicked: {state} at {System.DateTime.Now:HH:mm:ss}";
            }
        }
    }
}
