// UnityEvent Viewer: lists every persistent call found by UnityEventScanner, marks broken ones,
// and answers "who calls this object" for the current selection.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.EditorExtension
{
    public class UnityEventViewer : EditorWindow
    {
        private enum Grouping { Asset, Status, None }

        private class Node
        {
            public string label;
            public UnityEventRecord record;
        }

        private class ColumnInfo
        {
            public string title;
            public Func<Node, string> display;
            public Func<Node, string> full;
        }

        private static readonly Color BrokenColor = new(0.95f, 0.4f, 0.4f);
        private static readonly Color WarningColor = new(0.95f, 0.75f, 0.3f);

        [SerializeField] private bool brokenOnly = true;
        [SerializeField] private bool referencingSelection;
        [SerializeField] private Grouping grouping = Grouping.Asset;
        [SerializeField] private string search = string.Empty;

        private readonly List<UnityEventRecord> records = new();
        private readonly List<ColumnInfo> columnInfos = new();
        private MultiColumnTreeView tree;
        private Label summary;

        [MenuItem("Tools/UnityEvent Viewer")]
        public static void Open() => GetWindow<UnityEventViewer>("UnityEvent Viewer");

        private void OnEnable() => Selection.selectionChanged += OnSelectionChanged;
        private void OnDisable() => Selection.selectionChanged -= OnSelectionChanged;

        private bool pinging;

        private void OnSelectionChanged()
        {
            if (referencingSelection && !pinging)
                Refresh();
        }

        private void CreateGUI()
        {
            minSize = new Vector2(420, 200);

            var actions = new Toolbar();
            actions.Add(new ToolbarButton(() => SetRecords(UnityEventScanner.ScanOpenScenes())) { text = "Scan Open Scenes" });
            actions.Add(new ToolbarButton(ScanSelectedAssets) { text = "Scan Selected Assets", tooltip = "Prefabs and ScriptableObjects in the selected Project folders or files." });
            actions.Add(new ToolbarSpacer { flex = true });
            actions.Add(new ToolbarButton(Export) { text = "Export JSON" });
            rootVisualElement.Add(actions);

            var view = new Toolbar();
            view.Add(Toggle("Broken only", brokenOnly, v => brokenOnly = v));
            view.Add(Toggle("Calls to selection", referencingSelection, v => referencingSelection = v));
            view.Add(GroupMenu());
            var searchField = new ToolbarSearchField { value = search };
            searchField.style.flexGrow = 1;
            searchField.style.flexShrink = 1;
            searchField.style.minWidth = 80;
            searchField.style.width = StyleKeyword.Auto;
            searchField.RegisterValueChangedCallback(e => { search = e.newValue; Refresh(); });
            view.Add(searchField);
            rootVisualElement.Add(view);

            summary = new Label { style = { paddingLeft = 4, paddingTop = 2, paddingBottom = 2, whiteSpace = WhiteSpace.Normal } };
            rootVisualElement.Add(summary);

            tree = new MultiColumnTreeView { fixedItemHeight = 18, selectionType = SelectionType.Multiple, style = { flexGrow = 1 } };
            AddColumn("Status", 130, n => n.record == null ? n.label : n.record.status.ToString(), colorByStatus: true);
            AddColumn("Owner", 220, n => Abbreviate(n.record?.ownerPath), n => n.record?.ownerPath);
            AddColumn("Event", 160, n => n.record == null ? string.Empty : $"{n.record.eventPath}[{n.record.index}]");
            AddColumn("Target", 200, n => Abbreviate(n.record?.targetPath), n => n.record?.targetPath);
            AddColumn("Method", 200, n => n.record == null ? string.Empty : $"{ShortTypeName(n.record.targetType)}.{n.record.methodName}",
                n => n.record == null ? string.Empty : $"{n.record.targetType}.{n.record.methodName}");
            AddColumn("Argument", 120, n => n.record?.argument);
            AddColumn("Detail", 260, n => n.record?.detail);
            tree.selectedIndicesChanged += _ => PingSelected();
            tree.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            tree.RegisterCallback<ValidateCommandEvent>(e => { if (e.commandName == "Copy") e.StopPropagation(); });
            tree.RegisterCallback<ExecuteCommandEvent>(e => { if (e.commandName == "Copy") { CopySelectedRows(); e.StopPropagation(); } });
            rootVisualElement.Add(tree);
            Refresh();
        }

        private ToolbarToggle Toggle(string text, bool value, Action<bool> set)
        {
            var toggle = new ToolbarToggle { text = text, value = value };
            toggle.RegisterValueChangedCallback(e => { set(e.newValue); Refresh(); });
            return toggle;
        }

        private ToolbarMenu GroupMenu()
        {
            var menu = new ToolbarMenu { text = $"Group: {grouping}" };
            foreach (Grouping option in Enum.GetValues(typeof(Grouping)))
                menu.menu.AppendAction(option.ToString(), _ =>
                {
                    grouping = option;
                    menu.text = $"Group: {grouping}";
                    Refresh();
                }, _ => grouping == option ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            return menu;
        }

        private void AddColumn(string title, float width, Func<Node, string> display, Func<Node, string> full = null, bool colorByStatus = false)
        {
            var info = new ColumnInfo { title = title, display = display, full = full ?? display };
            columnInfos.Add(info);
            tree.columns.Add(new Column
            {
                name = title,
                title = title,
                width = width,
                makeCell = () => MakeCell(info),
                bindCell = (element, index) =>
                {
                    var node = tree.GetItemDataForIndex<Node>(index);
                    var label = (Label)element;
                    label.userData = index;
                    label.text = display(node) ?? string.Empty;
                    label.tooltip = info.full(node) ?? string.Empty;
                    label.style.unityFontStyleAndWeight = node.record == null ? FontStyle.Bold : FontStyle.Normal;
                    label.style.color = colorByStatus && node.record != null ? StatusColor(node.record.status) : StyleKeyword.Null;
                },
            });
        }

        private Label MakeCell(ColumnInfo info)
        {
            var gridColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.1f) : new Color(0, 0, 0, 0.15f);
            var label = new Label
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis,
                    flexGrow = 1,
                    height = Length.Percent(100),
                    paddingLeft = 4,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderRightColor = gridColor,
                    borderBottomColor = gridColor,
                },
            };
            label.RegisterCallback<ClickEvent>(_ =>
            {
                if (label.userData is int index)
                    Copy(info.full(tree.GetItemDataForIndex<Node>(index)), info.title);
            });
            return label;
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.C || !(e.ctrlKey || e.commandKey))
                return;
            CopySelectedRows();
            e.StopPropagation();
        }

        private void CopySelectedRows()
        {
            var rows = tree.selectedIndices
                .Select(i => string.Join("\t", columnInfos.Select(c => c.full(tree.GetItemDataForIndex<Node>(i)) ?? string.Empty)))
                .ToList();
            if (rows.Count > 0)
                Copy(string.Join("\n", rows), rows.Count == 1 ? "row" : $"{rows.Count} rows");
        }

        private void Copy(string text, string what)
        {
            if (text.IsEmpty())
                return;
            EditorGUIUtility.systemCopyBuffer = text;
            ShowNotification(new GUIContent($"Copied {what}"), 0.6);
        }

        /// <summary>
        /// Keeps only the last hierarchy level of "Root/.../Leaf (Type) > Inline" so long paths fit a column;
        /// the tooltip and copies keep the full path.
        /// </summary>
        private static string Abbreviate(string path)
        {
            if (path.IsEmpty())
                return string.Empty;
            var cut = new[] { path.IndexOf(" (", StringComparison.Ordinal), path.IndexOf(" > ", StringComparison.Ordinal) }
                .Where(i => i >= 0).DefaultIfEmpty(path.Length).Min();
            var slash = path.LastIndexOf('/', Math.Max(0, cut - 1));
            return slash < 0 ? path : $".../{path.Substring(slash + 1)}";
        }

        private static StyleColor StatusColor(PersistentCallStatus status) =>
            status == PersistentCallStatus.Valid ? StyleKeyword.Null :
            status == PersistentCallStatus.StaleTargetType ? WarningColor : BrokenColor;

        private static string ShortTypeName(string fullName)
        {
            if (fullName.IsEmpty())
                return "?";
            var generic = fullName.IndexOf('[');
            var head = generic < 0 ? fullName : fullName.Substring(0, generic);
            return head.Substring(head.LastIndexOf('.') + 1);
        }

        private void ScanSelectedAssets()
        {
            var paths = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).Where(p => !p.IsEmpty()).ToArray();
            if (paths.Length == 0)
            {
                ShowNotification(new GUIContent("Select folders, prefabs or assets in the Project window."));
                return;
            }
            SetRecords(UnityEventScanner.ScanAssets(paths));
        }

        private void SetRecords(List<UnityEventRecord> scanned)
        {
            records.Clear();
            records.AddRange(scanned);
            Refresh();
        }

        private bool Matches(UnityEventRecord r)
        {
            if (brokenOnly && r.status == PersistentCallStatus.Valid)
                return false;
            if (referencingSelection && !TargetsSelection(r))
                return false;
            if (search.IsEmpty())
                return true;
            return new[] { r.ownerPath, r.targetPath, r.targetType, r.methodName, r.eventPath, r.argument, r.assetPath }
                .Any(s => s != null && s.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool TargetsSelection(UnityEventRecord r)
        {
            if (r.target == null)
                return false;
            var targetObject = r.target is Component c ? c.gameObject : r.target;
            return Selection.objects.Any(s => s == r.target || s == targetObject);
        }

        private void Refresh()
        {
            if (tree == null)
                return;
            var id = 0;
            var visible = records.Where(Matches).OrderBy(r => r.status).ToList();
            IEnumerable<TreeViewItemData<Node>> Leaves(IEnumerable<UnityEventRecord> rs) =>
                rs.Select(r => new TreeViewItemData<Node>(id++, new Node { record = r }));
            var roots = grouping switch
            {
                Grouping.Asset => visible.GroupBy(r => r.assetPath).OrderBy(g => g.Key)
                    .Select(g => new TreeViewItemData<Node>(id++, new Node { label = $"{GroupLabel(g.Key)} ({g.Count()})" }, Leaves(g).ToList())).ToList(),
                Grouping.Status => visible.GroupBy(r => r.status).OrderBy(g => g.Key)
                    .Select(g => new TreeViewItemData<Node>(id++, new Node { label = $"{g.Key} ({g.Count()})" }, Leaves(g).ToList())).ToList(),
                _ => Leaves(visible).ToList(),
            };
            tree.SetRootItems(roots);
            tree.Rebuild();
            tree.ExpandAll();
            var broken = records.Count(r => r.IsBroken);
            summary.text = records.Count == 0
                ? "Scan open scenes, or select Project folders/assets and scan them."
                : $"{records.Count} calls, {broken} broken, {visible.Count} shown";
        }

        private static string GroupLabel(string assetPath) => assetPath.IsEmpty() ? "(unsaved scene)" : assetPath;

        private void PingSelected()
        {
            var node = tree.selectedIndex >= 0 ? tree.GetItemDataForIndex<Node>(tree.selectedIndex) : null;
            var owner = node?.record?.owner;
            if (owner == null)
                return;
            pinging = true;
            Selection.activeObject = owner;
            EditorGUIUtility.PingObject(owner);
            EditorApplication.delayCall += () => pinging = false;
        }

        private void Export()
        {
            if (records.Count == 0)
                return;
            var path = EditorUtility.SaveFilePanel("Export UnityEvent report", string.Empty, "UnityEventReport.json", "json");
            if (path.IsEmpty())
                return;
            UnityEventReport.Write(records.Where(Matches).ToList(), path);
            EditorUtility.RevealInFinder(path);
        }
    }
}
