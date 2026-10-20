using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// UI Toolkit implementation of Yu5h1Lib.UX OptionPicker (3.0.2).
    /// Uses native <see cref="Button"/> with <c>Yu5h1Button.uss</c> for trigger and option items,
    /// and a native <see cref="VisualElement"/> popup panel with <c>Yu5h1OptionPicker.uss</c>.
    /// </summary>
    public class OptionPicker : VisualElement
    {
        private const string ButtonStyleResource = "Yu5h1Lib.UIToolkit/Yu5h1Button";
        private const string OptionPickerStyleResource = "Yu5h1Lib.UIToolkit/Yu5h1OptionPicker";

        public OptionPickerModel Model { get; } = new OptionPickerModel();

        public OptionPickerButtonConfig TriggerConfig { get; set; } = OptionPickerButtonConfig.Default;
        public OptionPickerButtonConfig OptionConfig { get; set; } = OptionPickerButtonConfig.Default;
        public OptionPickerAppearance PanelAppearance { get; set; } = OptionPickerAppearance.Solid;
        public VisualElement PanelElement => panelElement;
        public HoldManipulator TriggerHoldManipulator => holdManipulator;

        private readonly Button triggerButton;
        private readonly HoldManipulator holdManipulator;
        private VisualElement panelElement;
        private readonly List<Button> optionButtons = new List<Button>();

        private StyleSheet buttonStyle;
        private StyleSheet optionPickerStyle;

        public OptionPicker()
        {
            buttonStyle = Resources.Load<StyleSheet>(ButtonStyleResource);
            optionPickerStyle = Resources.Load<StyleSheet>(OptionPickerStyleResource);

            if (optionPickerStyle != null)
            {
                styleSheets.Add(optionPickerStyle);
            }

            triggerButton = new Button();
            if (buttonStyle != null)
            {
                triggerButton.styleSheets.Add(buttonStyle);
            }
            triggerButton.AddToClassList("yu5h1-button");

            Add(triggerButton);

            holdManipulator = new HoldManipulator
            {
                HoldDurationMs = (long)(OptionPickerModel.DefaultHoldDuration * 1000f),
                DragThreshold = HoldManipulator.DefaultDragThreshold,
                CanStartHold = _ => Model.interaction != OptionPickerInteraction.Cycle
            };
            holdManipulator.HoldTriggered += OnTriggerHoldTriggered;
            holdManipulator.HoldMoved += OnTriggerHoldMoved;
            holdManipulator.HoldEnded += OnTriggerHoldEnded;
            holdManipulator.HoldCanceled += OnTriggerHoldCanceled;
            holdManipulator.TapTriggered += OnTriggerTapTriggered;
            triggerButton.AddManipulator(holdManipulator);

            triggerButton.RegisterCallback<KeyDownEvent>(OnTriggerKeyDown);

            Model.OnOpened += OnModelOpened;
            Model.OnClosed += OnModelClosed;
            Model.OnChosen += OnModelChosen;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            panel?.visualTree.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            panel?.visualTree.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
            holdManipulator?.Cancel();
            ClosePanel();
        }

        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (!Model.isOpen || panelElement == null) return;

            // 點擊面板與觸發鈕以外的地方關閉
            var target = evt.target as VisualElement;
            if (target != null && (panelElement.Contains(target) || triggerButton.Contains(target)))
            {
                return;
            }

            Model.Close();
        }

        public void SetOptions(IEnumerable<OptionItem> options, string selectedValue = null)
        {
            Model.SetOptions(options);
            if (selectedValue != null)
            {
                Model.currentValue = selectedValue;
            }
            UpdateTriggerDisplay();
        }

        public void SetValue(string value)
        {
            Model.currentValue = value;
            UpdateTriggerDisplay();
        }

        public void UpdateTriggerDisplay()
        {
            var current = Model.GetResolvedCurrentOption();
            ApplyButtonStyle(triggerButton, current, TriggerConfig);
        }

        private void ApplyButtonStyle(Button btn, OptionItem item, OptionPickerButtonConfig config)
        {
            btn.RemoveFromClassList("yu5h1-button--icon-only");
            btn.RemoveFromClassList("yu5h1-button--ghost");

            bool hasIcon = item != null && !item.icon.IsEmpty();
            bool displayIcon = config.showIcon && hasIcon;
            bool displayLabel = config.showLabel || !displayIcon;
            bool iconOnly = displayIcon && !displayLabel;

            if (displayIcon)
            {
                btn.iconImage = item.icon;
            }
            else
            {
                btn.iconImage = default;
            }

            if (displayLabel && item != null)
            {
                btn.text = item.label;
            }
            else
            {
                btn.text = string.Empty;
            }

            if (config.appearance == OptionPickerAppearance.Ghost)
            {
                btn.AddToClassList("yu5h1-button--ghost");
            }

            if (iconOnly)
            {
                btn.AddToClassList("yu5h1-button--icon-only");
                btn.style.width = config.size;
                btn.style.minWidth = config.size;
            }
            else
            {
                btn.style.width = StyleKeyword.Auto;
                btn.style.minWidth = StyleKeyword.Auto;
            }

            btn.style.height = config.size;
            btn.style.minHeight = config.size;
        }

        private void OnTriggerHoldTriggered(Vector2 screenPos)
        {
            Model.HandleTriggerHold();
            panelElement?.BringToFront();
            panel?.visualTree?.MarkDirtyRepaint();
            UpdateHoveredOption(screenPos);
        }

        private void OnTriggerHoldMoved(Vector2 screenPos)
        {
            UpdateHoveredOption(screenPos);
        }

        private void OnTriggerHoldEnded(Vector2 screenPos)
        {
            var hoveredOpt = GetOptionAtPosition(screenPos);
            if (hoveredOpt != null)
            {
                // 長按移至選項放開：選定該選項
                Model.Choose(hoveredOpt.id);
            }
            else if (triggerButton.worldBound.Contains(screenPos) ||
                     (panelElement != null && panelElement.worldBound.Contains(screenPos)))
            {
                // 原地放開或在面板內放開：保持面板開啟，供使用者直接點選
            }
            else
            {
                // 在面板與按鈕外放開：關閉
                Model.Close();
            }

            ClearHoveredOptions();
        }

        private void OnTriggerHoldCanceled()
        {
            ClearHoveredOptions();
            if (Model.isOpen)
            {
                Model.Close();
            }
        }

        private void OnTriggerTapTriggered()
        {
            Model.HandleTriggerTap();
            ClearHoveredOptions();
        }

        private void UpdateHoveredOption(Vector2 screenPos)
        {
            var picked = panel?.Pick(screenPos);
            for (int i = 0; i < optionButtons.Count; i++)
            {
                var btn = optionButtons[i];
                bool isDisabled = i < Model.Options.Count && Model.Options[i].disabled;
                bool isHovered = !isDisabled &&
                                 ((picked != null && (btn == picked || btn.Contains(picked))) ||
                                  btn.worldBound.Contains(screenPos) ||
                                  btn.ContainsPoint(btn.WorldToLocal(screenPos)));
                if (isHovered)
                {
                    btn.AddToClassList("yu5h1-button--hover");
                    btn.AddToClassList("yu5h1-optionpicker__option--hover");
                }
                else
                {
                    btn.RemoveFromClassList("yu5h1-button--hover");
                    btn.RemoveFromClassList("yu5h1-optionpicker__option--hover");
                }
            }
        }

        private void ClearHoveredOptions()
        {
            for (int i = 0; i < optionButtons.Count; i++)
            {
                optionButtons[i].RemoveFromClassList("yu5h1-button--hover");
                optionButtons[i].RemoveFromClassList("yu5h1-optionpicker__option--hover");
            }
        }

        private OptionItem GetOptionAtPosition(Vector2 screenPos)
        {
            if (panelElement == null || !Model.isOpen) return null;
            var picked = panel?.Pick(screenPos);
            for (int i = 0; i < optionButtons.Count; i++)
            {
                var btn = optionButtons[i];
                bool isOver = (picked != null && (btn == picked || btn.Contains(picked))) ||
                              btn.worldBound.Contains(screenPos) ||
                              btn.ContainsPoint(btn.WorldToLocal(screenPos));
                if (isOver)
                {
                    if (i < Model.Options.Count && !Model.Options[i].disabled)
                    {
                        return Model.Options[i];
                    }
                }
            }
            return null;
        }

        private void OnTriggerKeyDown(KeyDownEvent evt)
        {
            string key = evt.keyCode switch
            {
                KeyCode.Return => "Enter",
                KeyCode.Space => "Space",
                KeyCode.Escape => "Escape",
                KeyCode.Tab => "Tab",
                KeyCode.LeftArrow => "ArrowLeft",
                KeyCode.RightArrow => "ArrowRight",
                KeyCode.UpArrow => "ArrowUp",
                KeyCode.DownArrow => "ArrowDown",
                KeyCode.Home => "Home",
                KeyCode.End => "End",
                _ => null
            };

            if (key != null)
            {
                Model.HandleKey(key);
                evt.StopPropagation();
            }
        }

        private void OnModelOpened()
        {
            OpenPanel();
        }

        private void OnModelClosed()
        {
            ClosePanel();
        }

        private void OnModelChosen(string id)
        {
            SetValue(id);
        }

        private void OpenPanel()
        {
            ClosePanel();

            panelElement = new VisualElement();
            if (optionPickerStyle != null)
            {
                panelElement.styleSheets.Add(optionPickerStyle);
            }
            panelElement.AddToClassList("yu5h1-optionpicker__panel");

            if (Model.orientation == OptionPickerOrientation.Horizontal)
            {
                panelElement.AddToClassList("yu5h1-optionpicker__panel--horizontal");
            }
            else
            {
                panelElement.AddToClassList("yu5h1-optionpicker__panel--vertical");
            }

            if (PanelAppearance == OptionPickerAppearance.Solid)
            {
                panelElement.AddToClassList("yu5h1-optionpicker__panel--solid");
            }
            else
            {
                panelElement.AddToClassList("yu5h1-optionpicker__panel--ghost");
            }

            optionButtons.Clear();
            var resolvedCurrent = Model.GetResolvedCurrentOption();

            for (int i = 0; i < Model.Options.Count; i++)
            {
                var opt = Model.Options[i];
                var btn = new Button();
                if (buttonStyle != null)
                {
                    btn.styleSheets.Add(buttonStyle);
                }
                if (optionPickerStyle != null)
                {
                    btn.styleSheets.Add(optionPickerStyle);
                }
                btn.AddToClassList("yu5h1-button");
                btn.AddToClassList("yu5h1-optionpicker__option");
                if (i == Model.Options.Count - 1)
                {
                    btn.AddToClassList("yu5h1-optionpicker__option--last");
                }

                ApplyButtonStyle(btn, opt, OptionConfig);

                if (opt.disabled)
                {
                    btn.SetEnabled(false);
                }

                if (resolvedCurrent != null && opt.id == resolvedCurrent.id)
                {
                    btn.AddToClassList("yu5h1-button--selected");
                }

                string optId = opt.id;
                btn.clicked += () => Model.Choose(optId);

                panelElement.Add(btn);
                optionButtons.Add(btn);
            }

            // 加到根節點進行全域覆蓋排版
            var root = panel?.visualTree ?? this;
            root.Add(panelElement);

            // 立即給予估算位置，避免在 (0, 0) 閃爍或不渲染
            UpdatePanelPosition();

            panelElement.RegisterCallback<GeometryChangedEvent>(OnPanelGeometryChanged);
        }

        private void OnPanelGeometryChanged(GeometryChangedEvent evt)
        {
            UpdatePanelPosition();
        }

        private void UpdatePanelPosition()
        {
            if (panelElement == null) return;

            var root = panel?.visualTree ?? this;
            Rect viewportRect = new Rect(0, 0, root.resolvedStyle.width, root.resolvedStyle.height);
            if (float.IsNaN(viewportRect.width) || viewportRect.width <= 0 || float.IsNaN(viewportRect.height) || viewportRect.height <= 0)
            {
                viewportRect = new Rect(0, 0, 1920, 1080);
            }

            Rect triggerRect = triggerButton.worldBound;
            if (float.IsNaN(triggerRect.width) || triggerRect.width <= 0 || float.IsNaN(triggerRect.x))
            {
                float tw = float.IsNaN(triggerButton.layout.width) || triggerButton.layout.width <= 0 ? TriggerConfig.size : triggerButton.layout.width;
                float th = float.IsNaN(triggerButton.layout.height) || triggerButton.layout.height <= 0 ? TriggerConfig.size : triggerButton.layout.height;
                float tx = float.IsNaN(triggerButton.worldBound.x) ? 0 : triggerButton.worldBound.x;
                float ty = float.IsNaN(triggerButton.worldBound.y) ? 0 : triggerButton.worldBound.y;
                triggerRect = new Rect(tx, ty, tw, th);
            }

            Vector2 panelSize = new Vector2(panelElement.resolvedStyle.width, panelElement.resolvedStyle.height);
            if (float.IsNaN(panelSize.x) || panelSize.x <= 0 || float.IsNaN(panelSize.y) || panelSize.y <= 0)
            {
                float count = Model.Options.Count;
                if (Model.orientation == OptionPickerOrientation.Horizontal)
                {
                    panelSize = new Vector2(8f + count * OptionConfig.size + Math.Max(0, count - 1) * 4f, 8f + OptionConfig.size);
                }
                else
                {
                    panelSize = new Vector2(8f + OptionConfig.size, 8f + count * OptionConfig.size + Math.Max(0, count - 1) * 4f);
                }
            }

            // 計算選中項目在面板內的局部位置
            var resolvedCurrent = Model.GetResolvedCurrentOption();
            int selectedIndex = 0;
            if (resolvedCurrent != null)
            {
                for (int i = 0; i < Model.Options.Count; i++)
                {
                    if (Model.Options[i].id == resolvedCurrent.id)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            Rect selectedLocalRect = Rect.zero;
            if (selectedIndex >= 0 && selectedIndex < optionButtons.Count)
            {
                var selBtn = optionButtons[selectedIndex];
                float sx = selBtn.layout.x;
                float sy = selBtn.layout.y;
                float sw = selBtn.layout.width;
                float sh = selBtn.layout.height;
                if (!float.IsNaN(sw) && sw > 0 && !float.IsNaN(sh) && sh > 0)
                {
                    selectedLocalRect = new Rect(sx, sy, sw, sh);
                }
            }
            if (selectedLocalRect.width <= 0)
            {
                if (Model.orientation == OptionPickerOrientation.Horizontal)
                {
                    selectedLocalRect = new Rect(4f + selectedIndex * (OptionConfig.size + 4f), 4f, OptionConfig.size, OptionConfig.size);
                }
                else
                {
                    selectedLocalRect = new Rect(4f, 4f + selectedIndex * (OptionConfig.size + 4f), OptionConfig.size, OptionConfig.size);
                }
            }

            Vector2 pos = OptionPickerPlacementSolver.ComputePanelPosition(
                triggerRect,
                panelSize,
                selectedLocalRect,
                Model.placement,
                Model.orientation,
                viewportRect);

            panelElement.style.left = pos.x;
            panelElement.style.top = pos.y;
        }

        private void ClosePanel()
        {
            ClearHoveredOptions();
            if (panelElement != null)
            {
                panelElement.RemoveFromHierarchy();
                panelElement = null;
                optionButtons.Clear();
            }
        }
    }
}
