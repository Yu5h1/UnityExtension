using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Yu5h1Lib.UI.Acceptance;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>Captures the delivered UxAcceptance settings case at one logical unit per output pixel.</summary>
    public sealed class SettingsPanelVisualAcceptanceTests
    {
        private const int Width = 440;
        private const int Height = 720;
        private const float ColorSettleSeconds = 0.2f;
        private const float MinimumTextContrast = 4.5f;
        private const string CaseId = "settingspanel";
        private const string CaseDescription = "Config、Setting 與確認保存";
        private const string OutputOption = "-settingsVisualOutput";

        [UnityTest]
        public IEnumerator CaptureNarrowThemesAndOrientations()
        {
            string output = ReadOutputPath();
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null),
                "Start batchmode with graphics enabled.");
            Directory.CreateDirectory(output);

            var initialIds = UxUiModuleRegistry.AllCases.Select(item => item.Id).ToArray();
            var originalCase = UxUiModuleRegistry.Get(CaseId);
            var evidence = new List<string>();
            var errors = new List<string>();
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var cameraObject = new GameObject("Settings screenshot camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = Height / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(255, 0, 255, 255);
            camera.targetTexture = target;

            GameObject eventSystem = null;
            GameObject hostObject = null;
            try
            {
                if (EventSystem.current == null)
                    eventSystem = new GameObject("Settings screenshot event system", typeof(EventSystem));

                var schemes = new[]
                {
                    (Name: "light", Palette: UguiControlPalette.Light),
                    (Name: "dark", Palette: UguiControlPalette.Dark)
                };
                foreach (var (theme, palette) in schemes)
                {
                    hostObject = new GameObject("Settings screenshot host", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                    var canvas = hostObject.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1f;
                    var host = hostObject.AddComponent<UxUiAcceptanceHost>();
                    var demo = new UxUiSettingsDemoCase(palette, "Yu5h1Lib.VisualAcceptance." + Guid.NewGuid());
                    UxUiModuleRegistry.Register(demo, CaseDescription);
                    host.SelectCase(CaseId);
                    var scaler = hostObject.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = 1f;
                    canvas.scaleFactor = 1f;
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    yield return new WaitForSecondsRealtime(ColorSettleSeconds);

                    Capture(camera, target, output, $"closed-{theme}", errors);
                    if (!ReferenceEquals(host.CurrentCase, demo))
                    {
                        errors.Add($"{theme}: the acceptance host did not mount the settings case.");
                        hostObject.SetActive(false);
                        Object.Destroy(hostObject);
                        hostObject = null;
                        continue;
                    }

                    var directionButton = host.ContentContainer.GetComponentsInChildren<Button>()
                        .SingleOrDefault(button => button.name == "Tab direction");
                    InspectCase(host, directionButton, palette, theme, errors);
                    var panels = host.ContentContainer.GetComponentsInChildren<SettingsPanel>(true);
                    foreach (var direction in new[] { Tabs.Direction.Row, Tabs.Direction.Column })
                    {
                        if (direction == Tabs.Direction.Column && directionButton != null)
                            Submit(directionButton);

                        foreach (var panel in panels)
                        {
                            string presentation = panel.Groups == null ? "setting" : "config";
                            string name = $"{presentation}-{direction.ToString().ToLowerInvariant()}-{theme}";
                            Submit(panel.Opener);
                            if (panel.SaveBar.ModeToggle.isOn)
                                Submit(panel.SaveBar.ModeToggle);

                            yield return null;
                            Canvas.ForceUpdateCanvases();
                            yield return new WaitForSecondsRealtime(ColorSettleSeconds);

                            Capture(camera, target, output, name, errors);
                            InspectPanel(canvas, panel, palette, direction, name, evidence, errors);
                            panel.Close();
                        }
                    }

                    hostObject.SetActive(false);
                    Object.Destroy(hostObject);
                    hostObject = null;
                    yield return null;
                }
            }
            finally
            {
                if (hostObject != null)
                {
                    hostObject.SetActive(false);
                    Object.Destroy(hostObject);
                }
                if (eventSystem != null)
                    Object.Destroy(eventSystem);

                var addedIds = UxUiModuleRegistry.AllCases.Select(item => item.Id).Except(initialIds).ToArray();
                foreach (var id in addedIds)
                    UxUiModuleRegistry.Unregister(id);
                if (originalCase != null)
                    UxUiModuleRegistry.Register(originalCase, CaseDescription);

                camera.targetTexture = null;
                Object.Destroy(cameraObject);
                target.Release();
                Object.Destroy(target);
                File.WriteAllLines(Path.Combine(output, "geometry.txt"), evidence.Concat(errors));
            }

            Assert.That(evidence.Count, Is.EqualTo(8), string.Join(Environment.NewLine, errors));
            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
        }

        private static string ReadOutputPath()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int position = Array.IndexOf(arguments, OutputOption);
            if (position < 0)
                Assert.Ignore($"Supply {OutputOption} <directory> to run screenshot acceptance.");
            if (position + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[position + 1])
                || arguments[position + 1].StartsWith("-", StringComparison.Ordinal))
                Assert.Fail($"{OutputOption} needs an output directory.");
            return Path.GetFullPath(arguments[position + 1]);
        }

        private static void Submit(Selectable button) =>
            ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);

        private static void Capture(Camera camera, RenderTexture target, string output, string name, List<string> errors)
        {
            var previous = RenderTexture.active;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                texture.Apply(false);
                File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
            }
            catch (Exception exception)
            {
                errors.Add($"{name}: {exception.Message}");
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(texture);
            }
        }

        private static void InspectCase(UxUiAcceptanceHost host, Button direction,
            UguiControlPalette palette, string name, List<string> errors)
        {
            var background = host.ContentContainer.GetComponentsInChildren<Image>()
                .SingleOrDefault(image => image.name == "SettingsDemo");
            if (background == null || background.color != palette.PageBackground)
                errors.Add($"{name}: the settings case page is missing the supplied page background.");
            var label = direction != null ? direction.GetComponentInChildren<Text>() : null;
            if (label == null || label.color != palette.ItemText || direction.colors.normalColor != palette.PanelBackground)
                errors.Add($"{name}: the tab direction button is missing the supplied control colors.");
            else
                ReadTextContrast(direction, name, errors);
        }

        private static void InspectPanel(Canvas canvas, SettingsPanel panel, UguiControlPalette palette,
            Tabs.Direction direction, string name, List<string> evidence, List<string> errors)
        {
            if (!panel.IsOpen || panel.Tabs.SelectedId != "general" || panel.Tabs.Orientation != direction)
                errors.Add($"{name}: expected the open General tab in the requested orientation.");

            var canvasRect = ((RectTransform)canvas.transform).rect;
            var images = panel.GetComponentsInChildren<Image>();
            var dialog = images.SingleOrDefault(image => image.name == "Settings panel");
            var backdrop = images.SingleOrDefault(image => image.name == "Backdrop");
            var title = dialog != null
                ? dialog.GetComponentsInChildren<Text>().SingleOrDefault(label => label.name == "Title")
                : null;
            var viewport = panel.Tabs.GetComponentInChildren<ScrollRect>()?.viewport;
            var tab = panel.Tabs.ActiveButton != null ? (RectTransform)panel.Tabs.ActiveButton.transform : null;
            var mark = tab != null ? tab.GetComponentsInChildren<Image>().SingleOrDefault(image => image.name == "Selection") : null;
            if (dialog == null || backdrop == null || title == null || viewport == null || tab == null || mark == null)
            {
                errors.Add($"{name}: the visible panel is missing a dialog, backdrop, title or selected tab.");
                return;
            }

            var surfaceColor = ColorUtility.ToHtmlStringRGB(dialog.color);
            var overlayColor = ColorUtility.ToHtmlStringRGBA(backdrop.color);
            var tabContrast = ReadNavigationContrast(panel.Tabs, name, errors);
            var groupContrast = panel.Groups == null ? "-" : ReadNavigationContrast(panel.Groups, name, errors);
            evidence.Add(FormattableString.Invariant(
                $"{name}: canvas={canvasRect.width:F1}x{canvasRect.height:F1} scale={canvas.scaleFactor:F2} dialog={dialog.rectTransform.rect.width:F1}x{dialog.rectTransform.rect.height:F1} tab-viewport={viewport.rect.width:F1}x{viewport.rect.height:F1} selected={panel.Tabs.SelectedId} surface={surfaceColor} overlay={overlayColor} tab-text-contrast={tabContrast} group-text-contrast={groupContrast}"));
            if (Mathf.Abs(canvasRect.width - Width) > 1f || Mathf.Abs(canvasRect.height - Height) > 1f
                || !Mathf.Approximately(canvas.scaleFactor, 1f))
                errors.Add($"{name}: canvas dimensions do not match the narrow 1:1 capture target.");
            if (dialog.color != palette.PanelBackground || backdrop.color != palette.Overlay
                || title.color != palette.PanelHeading || mark.color != palette.Accent)
                errors.Add($"{name}: the panel surface, backdrop, title or selection marker is missing its palette role.");
            if (panel.SaveBar.ModeToggle.isOn || !panel.SaveBar.SaveButton.IsInteractable())
                errors.Add($"{name}: expected manual mode with enabled Apply.");
            if (tab.rect.width <= 1f || tab.rect.height <= 1f)
                errors.Add($"{name}: selected tab has no visible area.");
        }

        private static string ReadNavigationContrast(Component navigation, string capture, List<string> errors)
        {
            var buttons = navigation.GetComponentsInChildren<Button>();
            if (buttons.Length == 0)
                errors.Add($"{capture}: {navigation.name} has no visible buttons.");

            return string.Join(",", buttons.Select(button =>
                $"{button.name}:{ReadTextContrast(button, capture, errors):F2}"));
        }

        private static float ReadTextContrast(Button button, string capture, List<string> errors)
        {
            var label = button.GetComponentInChildren<Text>();
            var background = button.targetGraphic;
            if (background == null || label == null)
            {
                errors.Add($"{capture}: {button.name} is missing its background or label.");
                return 0f;
            }

            var fill = background.color * background.canvasRenderer.GetColor();
            var text = label.color * label.canvasRenderer.GetColor();
            float a = Luminance(fill);
            float b = Luminance(text);
            float ratio = (Mathf.Max(a, b) + 0.05f) / (Mathf.Min(a, b) + 0.05f);
            if (ratio < MinimumTextContrast)
                errors.Add($"{capture}: {button.name} text contrast is {ratio:F2} ({ColorUtility.ToHtmlStringRGB(text)} on {ColorUtility.ToHtmlStringRGB(fill)}).");
            return ratio;
        }

        private static float Luminance(Color color)
        {
            var linear = color.linear;
            return linear.r * 0.2126f + linear.g * 0.7152f + linear.b * 0.0722f;
        }
    }
}
