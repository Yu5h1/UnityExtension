using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib.UI.Acceptance;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>
    /// Verifies Yu5h1Lib.UX Button 2.0.0 rules (BTN-DISPLAY, BTN-SIZE, BTN-LAYOUT) on UGUI Button + ButtonAddon,
    /// and verifies the shared UGUI acceptance application host.
    /// </summary>
    public class UxUiButtonTests
    {
        private GameObject _canvasObject;
        private Texture2D _testTexture;
        private Sprite _testSprite;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasRect = _canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1280f, 720f);

            _testTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            _testSprite = Sprite.Create(_testTexture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 100f);
        }

        [TearDown]
        public void TearDown()
        {
            UxUiModuleRegistry.Clear();
            if (_canvasObject != null)
            {
                Object.DestroyImmediate(_canvasObject);
                _canvasObject = null;
            }
            if (_testSprite != null)
            {
                Object.DestroyImmediate(_testSprite);
                _testSprite = null;
            }
            if (_testTexture != null)
            {
                Object.DestroyImmediate(_testTexture);
                _testTexture = null;
            }
        }

        [Test]
        public void BT01_DisplayCombinationsAndFallback_WithAndWithoutSprite()
        {
            var button = ButtonAddon.Create(
                _canvasObject.transform,
                icon: _testSprite,
                label: "Action",
                appearance: ButtonAppearance.Solid,
                showIcon: true,
                showLabel: true,
                size: 32f);

            Assert.IsTrue(button.iconImage.gameObject.activeSelf);
            Assert.IsTrue(button.labelText.gameObject.activeSelf);
            Assert.AreEqual(32f, button.rectTransform.rect.height, 0.05f);
            Assert.Greater(button.rectTransform.rect.width, 32f);

            button.showLabel = false;
            Assert.IsTrue(button.iconImage.gameObject.activeSelf);
            Assert.IsFalse(button.labelText.gameObject.activeSelf);
            Assert.AreEqual(32f, button.rectTransform.rect.width, 0.05f);
            Assert.AreEqual(32f, button.rectTransform.rect.height, 0.05f);
            Vector2 btnCenter = button.rectTransform.TransformPoint(button.rectTransform.rect.center);
            Vector2 iconCenter = button.iconImage.rectTransform.TransformPoint(button.iconImage.rectTransform.rect.center);
            Assert.AreEqual(btnCenter.x, iconCenter.x, 0.05f);
            Assert.AreEqual(btnCenter.y, iconCenter.y, 0.05f);

            button.showIcon = false;
            button.showLabel = true;
            Assert.IsFalse(button.iconImage.gameObject.activeSelf);
            Assert.IsTrue(button.labelText.gameObject.activeSelf);
            Assert.AreEqual(32f, button.rectTransform.rect.height, 0.05f);

            button.showIcon = false;
            button.showLabel = false;
            Assert.IsFalse(button.iconImage.gameObject.activeSelf);
            Assert.IsTrue(button.labelText.gameObject.activeSelf);
            Assert.AreEqual(32f, button.rectTransform.rect.height, 0.05f);

            button.icon = null;
            button.showIcon = true;
            button.showLabel = false;
            Assert.IsFalse(button.iconImage.gameObject.activeSelf);
            Assert.IsTrue(button.labelText.gameObject.activeSelf);
            Assert.AreEqual(32f, button.rectTransform.rect.height, 0.05f);
        }

        [Test]
        public void BT03_SizesAndLayout_IconFitsButtonAndPrecedesLabelWith8pxGap()
        {
            float[] sizes = { 24f, 32f, 40f };

            for (int i = 0; i < sizes.Length; i++)
            {
                float s = sizes[i];
                float expectedIcon = s * ButtonAddon.IconRatio;

                var iconOnly = ButtonAddon.Create(
                    _canvasObject.transform,
                    icon: _testSprite,
                    label: "Action",
                    appearance: ButtonAppearance.Solid,
                    showIcon: true,
                    showLabel: false,
                    size: s);

                Assert.AreEqual(s, iconOnly.rectTransform.rect.width, 0.05f);
                Assert.AreEqual(s, iconOnly.rectTransform.rect.height, 0.05f);
                Assert.AreEqual(expectedIcon, iconOnly.iconImage.rectTransform.rect.width, 0.05f);
                Assert.AreEqual(expectedIcon, iconOnly.iconImage.rectTransform.rect.height, 0.05f);

                var withLabel = ButtonAddon.Create(
                    _canvasObject.transform,
                    icon: _testSprite,
                    label: "Action",
                    appearance: ButtonAppearance.Solid,
                    showIcon: true,
                    showLabel: true,
                    size: s);

                Assert.AreEqual(s, withLabel.rectTransform.rect.height, 0.05f);
                Assert.AreEqual(expectedIcon, withLabel.iconImage.rectTransform.rect.width, 0.05f);
                Assert.AreEqual(expectedIcon, withLabel.iconImage.rectTransform.rect.height, 0.05f);

                var iconRectTransform = withLabel.iconImage.rectTransform;
                var labelRectTransform = withLabel.labelText.rectTransform;

                Vector3 iconMaxWorld = iconRectTransform.TransformPoint(new Vector3(iconRectTransform.rect.xMax, iconRectTransform.rect.center.y, 0f));
                Vector3 labelMinWorld = labelRectTransform.TransformPoint(new Vector3(labelRectTransform.rect.xMin, labelRectTransform.rect.center.y, 0f));
                Vector3 iconMaxLocal = withLabel.rectTransform.InverseTransformPoint(iconMaxWorld);
                Vector3 labelMinLocal = withLabel.rectTransform.InverseTransformPoint(labelMinWorld);

                Assert.Less(iconMaxLocal.x, labelMinLocal.x);
                Assert.AreEqual(ButtonAddon.IconLabelGap, labelMinLocal.x - iconMaxLocal.x, 0.05f);
                Assert.AreEqual(iconMaxLocal.y, labelMinLocal.y, 0.05f);
            }
        }

        [Test]
        public void BT04_DxPrefabDelivery_InstantiatesWithMinimalTokens_AndPreservesInspectorAdjustability()
        {
            string prefabPath = "Packages/com.yu5h1.ui/Runtime/Prefabs/Button.prefab";
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Expected Button prefab at {prefabPath}");

            // 1. Token efficiency: downstream agent needs only 1 line to instantiate fully compliant button
            var buttonGo = Object.Instantiate(prefab, _canvasObject.transform);
            var addon = buttonGo.GetComponent<ButtonAddon>();
            Assert.IsNotNull(addon);

            var rootRect = buttonGo.GetComponent<RectTransform>();
            Assert.IsNotNull(rootRect);
            Assert.AreEqual(32f, rootRect.rect.height, 0.05f);

            var hlg = buttonGo.GetComponent<HorizontalLayoutGroup>();
            Assert.IsNotNull(hlg);
            Assert.AreEqual(8f, hlg.spacing, 0.05f);
            Assert.AreEqual(TextAnchor.MiddleCenter, hlg.childAlignment);

            var iconImage = addon.iconImage;
            var labelText = addon.labelText;
            Assert.IsNotNull(iconImage);
            Assert.IsNotNull(labelText);

            // 2. Inspector adjustability retention: properties can be customized directly
            addon.label = "Custom Action";
            Assert.AreEqual("Custom Action", labelText.text);

            addon.icon = _testSprite;
            addon.showIcon = true;
            Assert.IsTrue(iconImage.gameObject.activeSelf);
            Assert.AreEqual(24f, iconImage.rectTransform.rect.width, 0.05f);
            Assert.AreEqual(24f, iconImage.rectTransform.rect.height, 0.05f);

            addon.size = 40f;
            Assert.AreEqual(40f, rootRect.rect.height, 0.05f);
            Assert.AreEqual(30f, iconImage.rectTransform.rect.width, 0.05f); // 40 * 0.75

            addon.appearance = ButtonAppearance.Ghost;
            Assert.AreEqual(ButtonAppearance.Ghost, addon.appearance);

            // 3. Ghost variant prefab check
            string ghostPrefabPath = "Packages/com.yu5h1.ui/Runtime/Prefabs/Button-Ghost.prefab";
            var ghostPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ghostPrefabPath);
            Assert.IsNotNull(ghostPrefab, $"Expected Button-Ghost prefab at {ghostPrefabPath}");
            var ghostAddon = ghostPrefab.GetComponent<ButtonAddon>();
            Assert.IsNotNull(ghostAddon);
            Assert.AreEqual(ButtonAppearance.Ghost, ghostAddon.appearance);
        }

        [Test]
        public void AcceptanceHost_ResolvesDefaultButtonTarget_SupportsSecondModuleAndSurfacesBlockedForUnknownId()
        {
            Assert.AreEqual("button", UxUiAcceptanceTarget.ResolveCurrentTargetId());

            var secondCase = new DummyModuleDemoCase();
            UxUiModuleRegistry.Register(secondCase);
            Assert.IsTrue(UxUiModuleRegistry.Contains("dummy-second"));
            Assert.AreSame(secondCase, UxUiModuleRegistry.Get("dummy-second"));
            Assert.IsNull(UxUiModuleRegistry.Get("unknown-module-id"));

            var hostType = System.Type.GetType("Yu5h1Lib.UI.Acceptance.UxUiAcceptanceHost, Yu5h1Lib.UI.Samples.UxAcceptance");
            if (hostType != null)
            {
                var hostGo = new GameObject("Host", typeof(RectTransform));
                hostGo.transform.SetParent(_canvasObject.transform, false);
                var host = hostGo.AddComponent(hostType);

                hostType.GetMethod("InitializeHost")?.Invoke(host, null);
                var currentCaseProp = hostType.GetProperty("CurrentCase");
                var statusTextProp = hostType.GetProperty("StatusText");
                var contentContainerProp = hostType.GetProperty("ContentContainer");
                var selectCaseMethod = hostType.GetMethod("SelectCase");

                var activeCase = currentCaseProp?.GetValue(host) as IUxUiModuleDemoCase;
                Assert.IsNotNull(activeCase);
                Assert.AreEqual("button", activeCase.Id);
                var contentRect = contentContainerProp?.GetValue(host) as RectTransform;
                Assert.IsNotNull(contentRect);
                Assert.AreEqual(1, contentRect.childCount);
                StringAssert.StartsWith("Active Acceptance Target:", statusTextProp?.GetValue(host) as string);

                UxUiModuleRegistry.Register(secondCase);
                selectCaseMethod?.Invoke(host, new object[] { "dummy-second" });
                activeCase = currentCaseProp?.GetValue(host) as IUxUiModuleDemoCase;
                Assert.IsNotNull(activeCase);
                Assert.AreEqual("dummy-second", activeCase.Id);
                Assert.IsTrue(secondCase.IsMounted);
                Assert.AreEqual(1, contentRect.childCount);

                selectCaseMethod?.Invoke(host, new object[] { "unknown-module-id" });
                Assert.IsNull(currentCaseProp?.GetValue(host));
                Assert.IsFalse(secondCase.IsMounted);
                StringAssert.StartsWith("[Blocked]", statusTextProp?.GetValue(host) as string);
            }

            var esGo = new GameObject("TestEventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UIInputHandler), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            esGo.transform.SetParent(_canvasObject.transform, false);
            var standaloneModule = esGo.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            var uiInputHandler = esGo.GetComponent<UIInputHandler>();
            standaloneModule.inputOverride = uiInputHandler;

            Assert.AreSame(uiInputHandler, standaloneModule.input);
            Assert.DoesNotThrow(() =>
            {
                _ = standaloneModule.input.mousePosition;
                _ = standaloneModule.input.GetMouseButtonDown(0);
                _ = standaloneModule.input.GetAxisRaw("Horizontal");
            });
        }

        private sealed class DummyModuleDemoCase : IUxUiModuleDemoCase
        {
            private GameObject _mounted;

            public string Id => "dummy-second";
            public string DisplayName => "Dummy Second (1.0.0)";
            public string ModuleSpec => "Yu5h1Lib.UX/DummySecond";
            public bool IsMounted => _mounted != null;

            public void Mount(RectTransform container)
            {
                Unmount();
                _mounted = new GameObject("DummyRoot", typeof(RectTransform));
                _mounted.transform.SetParent(container, false);
            }

            public void Unmount()
            {
                if (_mounted != null)
                {
                    Object.DestroyImmediate(_mounted);
                    _mounted = null;
                }
            }
        }
    }
}
