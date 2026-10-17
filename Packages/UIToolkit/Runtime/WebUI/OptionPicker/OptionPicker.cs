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

        private readonly Button triggerButton;
        private VisualElement panelElement;
        private readonly List<Button> optionButtons = new List<Button>();

        private StyleSheet buttonStyle;
        private StyleSheet optionPickerStyle;

        private IVisualElementScheduledItem holdSchedule;
        private bool isHeld;
        private Vector2 pointerDownPos;

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

            triggerButton.RegisterCallback<PointerDownEvent>(OnTriggerPointerDown);
            triggerButton.RegisterCallback<PointerUpEvent>(OnTriggerPointerUp);
            triggerButton.RegisterCallback<PointerMoveEvent>(OnTriggerPointerMove);
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

        private void OnTriggerPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            isHeld = false;
            pointerDownPos = evt.position;

            holdSchedule?.Pause();
            holdSchedule = schedule.Execute(() =>
            {
                isHeld = true;
                Model.HandleTriggerHold();
            }).StartingIn((long)(OptionPickerModel.DefaultHoldDuration * 1000));
        }

        private void OnTriggerPointerMove(PointerMoveEvent evt)
        {
            if (holdSchedule != null && !isHeld)
            {
                // 若超出拖曳閾值（8px），取消長按計時
                if (Vector2.Distance(evt.position, pointerDownPos) > 8f)
                {
                    holdSchedule.Pause();
                }
            }
        }

        private void OnTriggerPointerUp(PointerUpEvent evt)
        {
            if (evt.button != 0) return;

            if (holdSchedule != null)
            {
                holdSchedule.Pause();
            }

            if (!isHeld)
            {
                Model.HandleTriggerTap();
            }
            else
            {
                // 長按放開：若面板已開啟且原地放開，選到目前的值（choice-hold-release-in-place-chooses-current）
                if (Model.isOpen && triggerButton.ContainsPoint(triggerButton.WorldToLocal(evt.position)))
                {
                    var cur = Model.GetResolvedCurrentOption();
                    if (cur != null)
                    {
                        Model.Choose(cur.id);
                    }
                }
            }
            isHeld = false;
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
            UpdateTriggerDisplay();
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

            panelElement.RegisterCallback<GeometryChangedEvent>(OnPanelGeometryChanged);
        }

        private void OnPanelGeometryChanged(GeometryChangedEvent evt)
        {
            panelElement.UnregisterCallback<GeometryChangedEvent>(OnPanelGeometryChanged);
            UpdatePanelPosition();
        }

        private void UpdatePanelPosition()
        {
            if (panelElement == null) return;

            var root = panel?.visualTree ?? this;
            Rect viewportRect = new Rect(0, 0, root.resolvedStyle.width, root.resolvedStyle.height);
            if (viewportRect.width <= 0 || viewportRect.height <= 0)
            {
                viewportRect = new Rect(0, 0, 1920, 1080);
            }

            Rect triggerRect = triggerButton.worldBound;
            Vector2 panelSize = new Vector2(panelElement.resolvedStyle.width, panelElement.resolvedStyle.height);

            // 計算選中項目在面板內的局部位置
            var resolvedCurrent = Model.GetResolvedCurrentOption();
            int selectedIndex = 0;
            if (resolvedCurrent != null)
            {
                selectedIndex = 0;
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
                selectedLocalRect = new Rect(selBtn.layout.x, selBtn.layout.y, selBtn.layout.width, selBtn.layout.height);
            }
            if (selectedLocalRect.width <= 0)
            {
                selectedLocalRect = new Rect(0, 0, OptionConfig.size, OptionConfig.size);
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
            if (panelElement != null)
            {
                panelElement.RemoveFromHierarchy();
                panelElement = null;
                optionButtons.Clear();
            }
        }
    }
}
