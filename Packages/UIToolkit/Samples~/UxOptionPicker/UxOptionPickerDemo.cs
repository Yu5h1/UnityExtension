using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1LibTest
{
    /// <summary>
    /// Visual and interaction sample for Yu5h1Lib.UX OptionPicker 3.0.2 on UI Toolkit.
    /// Demonstrates TaskProgress Visibility (picker) and Sorting (both) option pickers,
    /// with aligned/adjacent placement, solid/ghost panel appearances, and display combinations.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UxOptionPickerDemo : MonoBehaviour
    {
        private UIDocument document;
        private PanelSettings settings;
        private Texture2D[] iconTextures;

        private void Start()
        {
            document = GetComponent<UIDocument>();
            if (document == null)
            {
                document = gameObject.AddComponent<UIDocument>();
            }

            if (document.panelSettings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
#if UNITY_EDITOR
                var defaultTheme = UnityEditor.AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(
                    "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
                if (defaultTheme != null)
                {
                    settings.themeStyleSheet = defaultTheme;
                }
#endif
                document.panelSettings = settings;
            }

            CreateIcons();
            BuildUI(document.rootVisualElement);
        }

        private void OnDestroy()
        {
            if (iconTextures != null)
            {
                for (int i = 0; i < iconTextures.Length; i++)
                {
                    if (iconTextures[i] != null) Destroy(iconTextures[i]);
                }
                iconTextures = null;
            }
            if (settings != null)
            {
                Destroy(settings);
                settings = null;
            }
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
                        // simple geometric shape
                        bool filled = (i == 0 && (x >= 8 && x <= 24 && y >= 8 && y <= 24)) || // Square
                                      (i == 1 && (Mathf.Abs(x - 16) + Mathf.Abs(y - 16) <= 10)) || // Diamond
                                      (i == 2 && (Vector2.Distance(new Vector2(x, y), new Vector2(16, 16)) <= 8)); // Circle
                        iconTextures[i].SetPixel(x, y, filled ? c : Color.clear);
                    }
                }
                iconTextures[i].Apply();
            }
        }

        private void BuildUI(VisualElement root)
        {
            root.Clear();

            var container = new ScrollView();
            container.style.flexGrow = 1;
            container.style.paddingLeft = 32;
            container.style.paddingRight = 32;
            container.style.paddingTop = 24;
            container.style.paddingBottom = 24;
            container.style.backgroundColor = new Color(0.94f, 0.94f, 0.96f, 1f);
            root.Add(container);

            var title = new Label("Native OptionPicker Demo (Yu5h1Lib.UX OptionPicker 3.0.2)");
            title.style.fontSize = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            title.style.marginBottom = 16;
            container.Add(title);

            // 1. 可見性按鈕（Visibility: picker 模式，短按或長按開垂直對齊面板）
            var sectionVis = CreateSection("1. 可見性按鈕 (Visibility: interaction=picker, orientation=vertical, placement=aligned)", container);
            var pickerVis = new Yu5h1Lib.UIToolkit.OptionPicker();
            pickerVis.Model.interaction = Yu5h1Lib.UIToolkit.OptionPickerInteraction.Picker;
            pickerVis.Model.orientation = Yu5h1Lib.UIToolkit.OptionPickerOrientation.Vertical;
            pickerVis.Model.placement = Yu5h1Lib.UIToolkit.OptionPickerPlacement.Aligned;

            var visOptions = new List<Yu5h1Lib.UIToolkit.OptionItem>
            {
                new Yu5h1Lib.UIToolkit.OptionItem("enabled", "啟用", Background.FromTexture2D(iconTextures[0])),
                new Yu5h1Lib.UIToolkit.OptionItem("disabled_all", "全體關閉", Background.FromTexture2D(iconTextures[1])),
                new Yu5h1Lib.UIToolkit.OptionItem("forbidden", "禁用", Background.FromTexture2D(iconTextures[2]))
            };
            pickerVis.SetOptions(visOptions, "enabled");
            pickerVis.Model.OnChosen += id =>
            {
                pickerVis.SetValue(id);
                Debug.Log($"[Visibility OptionPicker] Chosen: {id}");
            };
            sectionVis.Add(pickerVis);

            // 2. 排序按鈕（Sorting: both 模式，短按循環、長按 400ms 開面板，相鄰放置）
            var sectionSort = CreateSection("2. 排序按鈕 (Sorting: interaction=both, orientation=vertical, placement=adjacent)", container);
            var pickerSort = new Yu5h1Lib.UIToolkit.OptionPicker();
            pickerSort.Model.interaction = Yu5h1Lib.UIToolkit.OptionPickerInteraction.Both;
            pickerSort.Model.orientation = Yu5h1Lib.UIToolkit.OptionPickerOrientation.Vertical;
            pickerSort.Model.placement = Yu5h1Lib.UIToolkit.OptionPickerPlacement.Adjacent;

            var sortOptions = new List<Yu5h1Lib.UIToolkit.OptionItem>
            {
                new Yu5h1Lib.UIToolkit.OptionItem("asc", "順排", Background.FromTexture2D(iconTextures[0])),
                new Yu5h1Lib.UIToolkit.OptionItem("desc", "逆排", Background.FromTexture2D(iconTextures[1])),
                new Yu5h1Lib.UIToolkit.OptionItem("custom", "自由排序", Background.FromTexture2D(iconTextures[2]))
            };
            pickerSort.SetOptions(sortOptions, "asc");
            pickerSort.Model.OnChosen += id =>
            {
                pickerSort.SetValue(id);
                Debug.Log($"[Sorting OptionPicker] Chosen: {id}");
            };
            sectionSort.Add(pickerSort);

            // 3. 工具列外觀（TaskProgress card-toolbar-icon: icon-only, ghost 面板與按鈕）
            var sectionToolbar = CreateSection("3. 工具列風格 (TaskProgress card-toolbar-icon: icon-only, ghost appearance)", container);
            var pickerToolbar = new Yu5h1Lib.UIToolkit.OptionPicker();
            pickerToolbar.Model.interaction = Yu5h1Lib.UIToolkit.OptionPickerInteraction.Both;
            pickerToolbar.Model.orientation = Yu5h1Lib.UIToolkit.OptionPickerOrientation.Horizontal;
            pickerToolbar.Model.placement = Yu5h1Lib.UIToolkit.OptionPickerPlacement.Aligned;
            pickerToolbar.PanelAppearance = Yu5h1Lib.UIToolkit.OptionPickerAppearance.Ghost;

            var toolbarConfig = new Yu5h1Lib.UIToolkit.OptionPickerButtonConfig
            {
                showIcon = true,
                showLabel = false,
                appearance = Yu5h1Lib.UIToolkit.OptionPickerAppearance.Ghost,
                size = 32f
            };
            pickerToolbar.TriggerConfig = toolbarConfig;
            pickerToolbar.OptionConfig = toolbarConfig;
            pickerToolbar.SetOptions(sortOptions, "asc");
            pickerToolbar.Model.OnChosen += id =>
            {
                pickerToolbar.SetValue(id);
                Debug.Log($"[Toolbar OptionPicker] Chosen: {id}");
            };
            sectionToolbar.Add(pickerToolbar);
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
    }
}
