using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Yu5h1Lib.UIToolkit;
using Yu5h1Lib.UIToolkit.Acceptance;

namespace Yu5h1LibTest
{
    public class UxOptionPickerDemoCase : IUxModuleDemoCase
    {
        public string Id => "optionpicker";
        public string DisplayName => "OptionPicker (3.0.2)";
        public string ModuleSpec => "Yu5h1Lib.UX/OptionPicker";

        private Texture2D[] iconTextures;
        private ScrollView containerScrollView;

        public void Mount(VisualElement container)
        {
            Unmount();

            CreateIcons();

            containerScrollView = new ScrollView();
            containerScrollView.style.flexGrow = 1;
            containerScrollView.style.paddingLeft = 32;
            containerScrollView.style.paddingRight = 32;
            containerScrollView.style.paddingTop = 24;
            containerScrollView.style.paddingBottom = 24;
            containerScrollView.style.backgroundColor = new Color(0.94f, 0.94f, 0.96f, 1f);
            container.Add(containerScrollView);

            var title = new Label("Native OptionPicker Demo (Yu5h1Lib.UX OptionPicker 3.0.2)");
            title.style.fontSize = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            title.style.marginBottom = 8;
            containerScrollView.Add(title);

            var subtitle = new Label("Demonstrates TaskProgress Visibility & Sorting, hold & slide, and aligned/adjacent placement.");
            subtitle.style.fontSize = 13;
            subtitle.style.color = new Color(0.4f, 0.44f, 0.52f, 1f);
            subtitle.style.marginBottom = 24;
            containerScrollView.Add(subtitle);

            var logLabel = new Label("Event log: Ready");
            logLabel.style.fontSize = 13;
            logLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            logLabel.style.color = new Color(0.28f, 0.32f, 0.42f, 1f);
            logLabel.style.marginBottom = 16;
            containerScrollView.Add(logLabel);

            // 1. 可見性按鈕（Visibility: picker 模式，短按或長按開垂直對齊面板）
            var sectionVis = CreateSection("1. 可見性按鈕 (Visibility: interaction=picker, orientation=vertical, placement=aligned)", containerScrollView);
            var pickerVis = new OptionPicker();
            pickerVis.Model.interaction = OptionPickerInteraction.Picker;
            pickerVis.Model.orientation = OptionPickerOrientation.Vertical;
            pickerVis.Model.placement = OptionPickerPlacement.Aligned;

            var visOptions = new List<OptionItem>
            {
                new OptionItem("enabled", "啟用", Background.FromTexture2D(iconTextures[0])),
                new OptionItem("disabled_all", "全體關閉", Background.FromTexture2D(iconTextures[1])),
                new OptionItem("forbidden", "禁用", Background.FromTexture2D(iconTextures[2]))
            };
            pickerVis.SetOptions(visOptions, "enabled");
            pickerVis.Model.OnChosen += id =>
            {
                pickerVis.SetValue(id);
                logLabel.text = $"[Visibility OptionPicker] Chosen: {id} at {System.DateTime.Now:HH:mm:ss}";
            };
            sectionVis.Add(pickerVis);

            // 2. 排序按鈕（Sorting: both 模式，短按循環、長按 400ms 開面板，相鄰放置）
            var sectionSort = CreateSection("2. 排序按鈕 (Sorting: interaction=both, orientation=vertical, placement=adjacent)", containerScrollView);
            var pickerSort = new OptionPicker();
            pickerSort.Model.interaction = OptionPickerInteraction.Both;
            pickerSort.Model.orientation = OptionPickerOrientation.Vertical;
            pickerSort.Model.placement = OptionPickerPlacement.Adjacent;

            var sortOptions = new List<OptionItem>
            {
                new OptionItem("asc", "順排", Background.FromTexture2D(iconTextures[0])),
                new OptionItem("desc", "逆排", Background.FromTexture2D(iconTextures[1])),
                new OptionItem("custom", "自由排序", Background.FromTexture2D(iconTextures[2]))
            };
            pickerSort.SetOptions(sortOptions, "asc");
            pickerSort.Model.OnChosen += id =>
            {
                pickerSort.SetValue(id);
                logLabel.text = $"[Sorting OptionPicker] Chosen: {id} at {System.DateTime.Now:HH:mm:ss}";
            };
            sectionSort.Add(pickerSort);

            // 3. 工具列圖示按鈕（Toolbar: interaction=picker, orientation=horizontal, placement=aligned, appearance=ghost）
            var sectionToolbar = CreateSection("3. 工具列按鈕 (Toolbar: interaction=picker, orientation=horizontal, placement=aligned, appearance=ghost)", containerScrollView);
            var pickerToolbar = new OptionPicker();
            pickerToolbar.Model.interaction = OptionPickerInteraction.Picker;
            pickerToolbar.Model.orientation = OptionPickerOrientation.Horizontal;
            pickerToolbar.Model.placement = OptionPickerPlacement.Aligned;
            pickerToolbar.PanelAppearance = OptionPickerAppearance.Ghost;

            var toolbarConfig = new OptionPickerButtonConfig
            {
                showIcon = true,
                showLabel = false,
                appearance = OptionPickerAppearance.Ghost,
                size = 32f
            };
            pickerToolbar.TriggerConfig = toolbarConfig;
            pickerToolbar.OptionConfig = toolbarConfig;
            pickerToolbar.SetOptions(sortOptions, "asc");
            pickerToolbar.Model.OnChosen += id =>
            {
                pickerToolbar.SetValue(id);
                logLabel.text = $"[Toolbar OptionPicker] Chosen: {id} at {System.DateTime.Now:HH:mm:ss}";
            };
            sectionToolbar.Add(pickerToolbar);

            // 4. 點擊直接切換按鈕（Cycle: interaction=cycle，點擊直接切換下一個項目）
            var sectionCycle = CreateSection("4. 點擊循環按鈕 (Cycle: interaction=cycle, 點擊直接切項目)", containerScrollView);
            var pickerCycle = new OptionPicker();
            pickerCycle.Model.interaction = OptionPickerInteraction.Cycle;
            var cycleOptions = new List<OptionItem>
            {
                new OptionItem("mode_1", "模式一 (方)", Background.FromTexture2D(iconTextures[0])),
                new OptionItem("mode_2", "模式二 (菱)", Background.FromTexture2D(iconTextures[1])),
                new OptionItem("mode_3", "模式三 (圓)", Background.FromTexture2D(iconTextures[2]))
            };
            pickerCycle.SetOptions(cycleOptions, "mode_1");
            pickerCycle.Model.OnChosen += id =>
            {
                logLabel.text = $"[Cycle OptionPicker] Cycled to: {id} at {System.DateTime.Now:HH:mm:ss}";
            };
            sectionCycle.Add(pickerCycle);
        }

        public void Unmount()
        {
            if (containerScrollView != null)
            {
                containerScrollView.RemoveFromHierarchy();
                containerScrollView = null;
            }
            if (iconTextures != null)
            {
                for (int i = 0; i < iconTextures.Length; i++)
                {
                    if (iconTextures[i] != null) Object.Destroy(iconTextures[i]);
                }
                iconTextures = null;
            }
        }

        private static VisualElement CreateSection(string titleText, VisualElement parent)
        {
            var header = new Label(titleText);
            header.style.fontSize = 13;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.25f, 0.28f, 0.35f, 1f);
            header.style.marginTop = 14;
            header.style.marginBottom = 6;
            parent.Add(header);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 6;
            parent.Add(row);

            return row;
        }

        private void CreateIcons()
        {
            iconTextures = new Texture2D[3];
            for (int i = 0; i < 3; i++)
            {
                iconTextures[i] = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var c = new Color(0.18f, 0.20f, 0.24f, 1f);
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        bool filled = (i == 0 && (x >= 8 && x <= 24 && y >= 8 && y <= 24)) || // Square
                                      (i == 1 && (Mathf.Abs(x - 16) + Mathf.Abs(y - 16) <= 10)) || // Diamond
                                      (i == 2 && (Vector2.Distance(new Vector2(x, y), new Vector2(16, 16)) <= 8)); // Circle
                        iconTextures[i].SetPixel(x, y, filled ? c : Color.clear);
                    }
                }
                iconTextures[i].Apply();
            }
        }
    }
}
