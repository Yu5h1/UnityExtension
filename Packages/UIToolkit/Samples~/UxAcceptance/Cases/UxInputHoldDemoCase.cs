using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Yu5h1Lib.UIToolkit;
using Yu5h1Lib.UIToolkit.Acceptance;

namespace Yu5h1LibTest
{
    /// <summary>
    /// Interactive acceptance case for Yu5h1Lib.UX Foundation/Input (3.0.1) HoldManipulator:
    /// demonstrates Tap vs Hold (400ms / 6px), native Click suppression, and Hold-and-Slide selection.
    /// </summary>
    public class UxInputHoldDemoCase : IUxModuleDemoCase
    {
        private const string ButtonStyleResource = "Yu5h1Lib.UIToolkit/Yu5h1Button";

        public string Id => "input";
        public string DisplayName => "Input / HoldManipulator (3.0.1)";
        public string ModuleSpec => "Yu5h1Lib.UX/Foundation/Input";

        private StyleSheet buttonStyle;
        private ScrollView containerScrollView;
        private readonly List<VisualElement> slideTargets = new List<VisualElement>();

        public void Mount(VisualElement container)
        {
            Unmount();

            buttonStyle = Resources.Load<StyleSheet>(ButtonStyleResource);

            containerScrollView = new ScrollView();
            containerScrollView.style.flexGrow = 1;
            containerScrollView.style.paddingLeft = 24;
            containerScrollView.style.paddingRight = 24;
            containerScrollView.style.paddingTop = 24;
            containerScrollView.style.paddingBottom = 24;
            containerScrollView.style.backgroundColor = new Color(0.94f, 0.94f, 0.96f, 1f);
            container.Add(containerScrollView);

            var title = new Label("HoldManipulator Demo (Yu5h1Lib.UX Foundation/Input 3.0.1)");
            title.style.fontSize = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            title.style.marginBottom = 8;
            containerScrollView.Add(title);

            var subtitle = new Label("holdDuration = 400ms | dragThreshold = 6px (local logical px) | Click suppression on hold/cancel");
            subtitle.style.fontSize = 12;
            subtitle.style.color = new Color(0.35f, 0.40f, 0.48f, 1f);
            subtitle.style.marginBottom = 16;
            containerScrollView.Add(subtitle);

            var logLabel = new Label("Status: Ready — try short tap, 400ms hold, or move > 6px before 400ms.");
            logLabel.style.fontSize = 13;
            logLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            logLabel.style.color = new Color(0.15f, 0.30f, 0.55f, 1f);
            logLabel.style.marginBottom = 16;
            containerScrollView.Add(logLabel);

            // 1. Tap vs Hold & Click Suppression
            var sectionTapHold = CreateSection("1. 短按與長按抑制 (INPUT-PRESS-TAP / INPUT-PRESS-HOLD / INPUT-PRESS-SUPPRESS)", containerScrollView);
            var tapHoldBtn = CreateStyledButton("Tap or Hold (400ms)");
            var stateBadge = new Label("State: Idle");
            stateBadge.style.marginLeft = 12;
            stateBadge.style.fontSize = 13;
            stateBadge.style.color = new Color(0.25f, 0.28f, 0.35f, 1f);

            var holdManipulator = new HoldManipulator();
            holdManipulator.HoldStarted += _ =>
            {
                stateBadge.text = "State: Pressing (< 400ms)...";
                logLabel.text = "HoldStarted: press began (move > 6px cancels hold).";
            };
            holdManipulator.HoldTriggered += pos =>
            {
                tapHoldBtn.AddToClassList("yu5h1-button--selected");
                stateBadge.text = "State: HOLDING (Pointer Captured)";
                logLabel.text = $"HoldTriggered at ({pos.x:0}, {pos.y:0}) — native click will be suppressed on release.";
            };
            holdManipulator.HoldMoved += pos =>
            {
                stateBadge.text = $"State: HOLDING & Sliding ({pos.x:0}, {pos.y:0})";
            };
            holdManipulator.HoldEnded += pos =>
            {
                tapHoldBtn.RemoveFromClassList("yu5h1-button--selected");
                stateBadge.text = "State: Hold Ended (Click Suppressed)";
                logLabel.text = $"HoldEnded at ({pos.x:0}, {pos.y:0}).";
            };
            holdManipulator.HoldCanceled += () =>
            {
                tapHoldBtn.RemoveFromClassList("yu5h1-button--selected");
                stateBadge.text = "State: Canceled (> 6px or interrupted)";
                logLabel.text = "HoldCanceled: gesture abandoned and click suppressed.";
            };
            holdManipulator.TapTriggered += () =>
            {
                stateBadge.text = "State: Short Tap Activated!";
                logLabel.text = "TapTriggered: released inside half-open bounds before 400ms.";
            };
            tapHoldBtn.AddManipulator(holdManipulator);

            sectionTapHold.Add(tapHoldBtn);
            sectionTapHold.Add(stateBadge);

            // 2. Hold-and-Slide Selection (INPUT-CHOICE-HOLD)
            var sectionSlide = CreateSection("2. 按住滑選 (INPUT-CHOICE-HOLD: Hold 400ms -> Slide to Zone -> Release)", containerScrollView);
            var slideTrigger = CreateStyledButton("Hold & Slide to Zone ->");
            slideTargets.Clear();

            string[] zoneNames = { "Zone Alpha", "Zone Beta", "Zone Gamma" };
            for (int i = 0; i < zoneNames.Length; i++)
            {
                var zone = CreateStyledButton(zoneNames[i]);
                zone.name = zoneNames[i];
                slideTargets.Add(zone);
            }

            var slideHold = new HoldManipulator();
            slideHold.HoldTriggered += pos =>
            {
                slideTrigger.AddToClassList("yu5h1-button--selected");
                UpdateHoveredZone(pos);
                logLabel.text = "Hold-and-Slide active: drag pointer over Zone Alpha / Beta / Gamma and release.";
            };
            slideHold.HoldMoved += pos =>
            {
                var hovered = UpdateHoveredZone(pos);
                logLabel.text = hovered != null
                    ? $"Sliding over: {hovered.name}"
                    : $"Sliding at ({pos.x:0}, {pos.y:0})";
            };
            slideHold.HoldEnded += pos =>
            {
                slideTrigger.RemoveFromClassList("yu5h1-button--selected");
                var chosen = UpdateHoveredZone(pos);
                ClearZoneHover();
                if (chosen != null)
                {
                    foreach (var z in slideTargets)
                    {
                        z.RemoveFromClassList("yu5h1-button--selected");
                    }
                    chosen.AddToClassList("yu5h1-button--selected");
                    logLabel.text = $"Hold-and-Slide Chosen: {chosen.name}";
                }
                else
                {
                    logLabel.text = "Hold-and-Slide released outside zones.";
                }
            };
            slideHold.HoldCanceled += () =>
            {
                slideTrigger.RemoveFromClassList("yu5h1-button--selected");
                ClearZoneHover();
            };
            slideTrigger.AddManipulator(slideHold);

            sectionSlide.Add(slideTrigger);
            foreach (var z in slideTargets)
            {
                sectionSlide.Add(z);
            }
        }

        private VisualElement UpdateHoveredZone(Vector2 screenPos)
        {
            VisualElement matched = null;
            for (int i = 0; i < slideTargets.Count; i++)
            {
                var z = slideTargets[i];
                bool over = z.worldBound.Contains(screenPos) || z.ContainsPoint(z.WorldToLocal(screenPos));
                if (over)
                {
                    z.AddToClassList("yu5h1-button--hover");
                    matched = z;
                }
                else
                {
                    z.RemoveFromClassList("yu5h1-button--hover");
                }
            }
            return matched;
        }

        private void ClearZoneHover()
        {
            for (int i = 0; i < slideTargets.Count; i++)
            {
                slideTargets[i].RemoveFromClassList("yu5h1-button--hover");
            }
        }

        public void Unmount()
        {
            slideTargets.Clear();
            if (containerScrollView != null)
            {
                containerScrollView.RemoveFromHierarchy();
                containerScrollView = null;
            }
        }

        private VisualElement CreateSection(string titleText, VisualElement parent)
        {
            var header = new Label(titleText);
            header.style.fontSize = 14;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.25f, 0.28f, 0.35f, 1f);
            header.style.marginTop = 12;
            header.style.marginBottom = 8;
            parent.Add(header);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 16;
            parent.Add(row);

            return row;
        }

        private Button CreateStyledButton(string text)
        {
            var btn = new Button { text = text };
            if (buttonStyle != null)
            {
                btn.styleSheets.Add(buttonStyle);
            }
            btn.AddToClassList("yu5h1-button");
            btn.style.height = 32;
            btn.style.minHeight = 32;
            btn.style.marginRight = 12;
            return btn;
        }
    }
}
