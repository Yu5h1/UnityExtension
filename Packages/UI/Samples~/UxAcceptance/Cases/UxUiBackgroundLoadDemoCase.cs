using UnityEngine;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>
    /// Shared acceptance demo case for Yu5h1Lib.UX BackgroundLoad (0.1.2-draft) on UGUI: <see cref="ContentSlot"/> and
    /// <see cref="ContentSlotView"/> from Yu5h1Lib.Common, fed by a simulated loader whose speed and failures are
    /// controlled on screen, so every state stays visible long enough to judge. The layout is built in
    /// <see cref="UxUiBackgroundLoadDemo"/>.
    /// </summary>
    public class UxUiBackgroundLoadDemoCase : IUxUiModuleDemoCase
    {
        public string Id => "backgroundload";
        public string DisplayName => "BackgroundLoad (0.1.2-draft)";
        public string ModuleSpec => "Yu5h1Lib.UX/Foundation/BackgroundLoad";

        private GameObject _rootPanel;

        public void Mount(RectTransform container)
        {
            Unmount();
            if (container == null)
                return;
            _rootPanel = new GameObject("BackgroundLoadDemo", typeof(RectTransform));
            var rect = (RectTransform)_rootPanel.transform;
            rect.SetParent(container, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _rootPanel.AddComponent<UxUiBackgroundLoadDemo>();
        }

        public void Unmount()
        {
            if (_rootPanel == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(_rootPanel);
            else
                Object.DestroyImmediate(_rootPanel);
            _rootPanel = null;
        }
    }
}
