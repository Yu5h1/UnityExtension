using UnityEngine;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>
    /// Shared acceptance demo case for Yu5h1Lib.UX Button (2.0.0) in Yu5h1Lib.UI (UGUI).
    /// The layout lives in the <c>Resources/UxAcceptance/ButtonDemo</c> Prefab so it can be adjusted in the Editor.
    /// </summary>
    public class UxUiButtonDemoCase : IUxUiModuleDemoCase
    {
        private const string DemoPrefabPath = "UxAcceptance/ButtonDemo";

        public string Id => "button";
        public string DisplayName => "Button (2.0.0)";
        public string ModuleSpec => "Yu5h1Lib.UX/Button";

        private GameObject _rootPanel;

        public void Mount(RectTransform container)
        {
            Unmount();

            if (container == null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(DemoPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Button demo Prefab not found at Resources/{DemoPrefabPath}.");
                return;
            }

            _rootPanel = Object.Instantiate(prefab, container, false);
            _rootPanel.name = prefab.name;
        }

        public void Unmount()
        {
            if (_rootPanel == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Object.Destroy(_rootPanel);
            }
            else
            {
                Object.DestroyImmediate(_rootPanel);
            }
            _rootPanel = null;
        }
    }
}
