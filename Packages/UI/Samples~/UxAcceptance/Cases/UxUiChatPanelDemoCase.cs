
using UnityEngine;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Mounts a self-contained chat message-area demo in the shared uGUI acceptance scene.</summary>
    public sealed class UxUiChatPanelDemoCase : IUxUiModuleDemoCase
    {
        private const string DemoPrefabPath = "UxAcceptance/ChatPanelDemo";

        public string Id => "chatpanel";
        public string DisplayName => "ChatPanel (0.4.0-draft)";
        public string ModuleSpec => "Yu5h1Lib.UX/Modules/ChatPanel";

        private GameObject root;

        public void Mount(RectTransform container)
        {
            Unmount();
            if (!ResourcesUtility.TryLoad(DemoPrefabPath, out GameObject prefab) ||
                !prefab.TryGetComponent<UxUiChatPanelDemo>(out var demoPrefab))
                throw new System.InvalidOperationException($"找不到聊天展示 Prefab Resources/{DemoPrefabPath}。");

            var demo = Object.Instantiate(demoPrefab, container, false);
            root = demo.gameObject;
            root.name = prefab.name;
            demo.Build();
            var config = ChatTextPerformanceSettings.Mount((RectTransform)root.transform, demo.Panel,
                "yu5h1lib.ui.acceptance.chat.text-performance.v1", UguiControlPalette.Dark);
            config.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            var content = root.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            var padding = content.padding;
            content.padding = new RectOffset(padding.left, padding.right, padding.top + 36, padding.bottom);
            ((RectTransform)config.Opener.transform).anchoredPosition = new Vector2(-padding.right, -padding.top);
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
