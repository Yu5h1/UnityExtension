
using UnityEngine;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Mounts a self-contained chat message-area demo in the shared uGUI acceptance scene.</summary>
    public sealed class UxUiChatPanelDemoCase : IUxUiModuleDemoCase
    {
        public string Id => "chatpanel";
        public string DisplayName => "ChatPanel (0.4.0-draft)";
        public string ModuleSpec => "Yu5h1Lib.UX/Modules/ChatPanel";

        private GameObject root;

        public void Mount(RectTransform container)
        {
            Unmount();
            root = new GameObject("ChatPanelDemo", typeof(RectTransform));
            root.transform.SetParent(container, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.AddComponent<UxUiChatPanelDemo>().Build();
        }

        public void Unmount()
        {
            if (root == null)
                return;
            root.SetActive(false);
            if (Application.isPlaying)
                Object.Destroy(root);
            else
                Object.DestroyImmediate(root);
            root = null;
        }
    }
}
