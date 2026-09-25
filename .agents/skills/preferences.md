# Preferences

Persist settings to PlayerPrefs with what Yu5h1Lib already ships. Load this before writing any save/load code for settings or control values: a uGUI-edited value is wired in the scene over MCP, not coded.

Members, design decisions, known defects, weaknesses, and open decisions: [偏好設定](../../Documentation/偏好設定.md). Load it only when changing the system, hitting a trap listed below, or extending it. Code-level invariants for editing the binding source live in `yu5h1lib-conventions` (`csharp-core.md`).

## Choose the approach

| Situation | Use |
|---|---|
| A uGUI `Toggle`, `Slider`, `InputField`, `TMP_InputField`, or an `OptionSet` edits the value | `Preferences`: [scene procedure](#scene-procedure-mcp) below. `TMP_InputField` binds only when the project references `com.yu5h1.tmpextension` (`Packages/Plugins/TMP`); without it the control is silently skipped |
| An option switched by `OptionSelector` | Bind the `OptionSet` to save the item's value, or the `OptionSelector` to save the index. Only one per GameObject: both use the GameObject name as key |
| Code owns the value and no control edits it | Call `PlayerPrefs` directly (`JsonUtility` for complex types). Do not add a per-value wrapper type |
| `ObservablePref<T>`, `PlayerPrefValue<T>`, `PlayerPrefObject<T>`, `AudioVolumePrefs` | Deprecated and scheduled for removal. Never use them in new work; plan § 移除計畫 |
| `Dropdown` / `TMP_Dropdown` | Not bindable yet (silently ignored). Plan decision D3 |
| UI Toolkit control | Not supported. Report the gap and route to plan § UI Toolkit 適用分析; do not hand-write a replacement |
| Password, token, or other secret | Not in PlayerPrefs: it is plaintext |
| A new uGUI control type must bind | Write one adapter (copy `ToggleAdapter`); `Preferences` needs no change |

## Scene procedure (MCP)

1. **Host.** Find an existing host with `find_gameobjects` by component `PlayerPreferences`, or a project subclass (grep the project for `: Preferences<`). If none, create an always-active GameObject and `manage_components add PlayerPreferences`. One host per Preferences type per scene. The host must be active at load: binding runs once in its `Awake`, and `instance` on a missing host auto-creates an empty one with no bindings.
2. **Keys.** Each control's GameObject name is its saved key. Rename with `manage_gameobject modify new_name` to a stable, meaningful name (`MusicEnabled`, `MasterVolume`), unique within the host, case-insensitive. Treat names as a persistent contract: a later rename orphans saved values.
3. **Defaults.** Put authoritative defaults in the host's `defaultSetting` (a DataView: field name → string value, in the adapter's format). Set each control's scene value (`isOn`, `value`, `text`) with `manage_components set_property` only as the last fallback, and keep it equal to `defaultSetting` when both exist so the scene shows what a new player gets. Every field follows "save → `defaultSetting` → control": a save's own value wins; a field the save lacks takes `defaultSetting` if `defaultSetting` has it, else the control's current value. Loading a save also merges in any field `defaultSetting` has that the save lacks. Whatever fills a missing field is written to the save at once. Changing `defaultSetting` or a scene value later does not touch fields an existing save already has.
4. **Register.** In the Editor, select the target controls in the Hierarchy, then use the Preferences component's (or any Component's) gear-icon CONTEXT menu → **綁定選取的控件**; it only appears on a Component that has a `_bindings` field. That command, the `_bindings` drawer, and MCP all share one Editor API, `Yu5h1Lib.EditorExtension.PreferencesBindingUtility.BindSelected(host, controls)`: it accepts a control that is itself an `IValuePort`, else one the live adapter registry covers, and reports each control as `Bound`, `AlreadyBound`, `DuplicateName`, or `Unbindable`.

   The CONTEXT menu itself needs a `MenuCommand` to supply `context`, which MCP's `execute_menu_item` cannot provide — call the API directly with `execute_code` instead. It still addresses objects by scene path, because Unity 6.3 rejects `GetInstanceID()` and `EditorUtility.InstanceIDToObject(int)` as obsolete compile errors; `Transform.Find` also reaches inactive children.

   ```csharp
   string prefsPath = "PlayerPreferences";   // GameObject holding the Preferences component
   string[] controlPaths = { };              // GameObjects holding the controls, e.g. "Canvas/SettingsPanel/MasterVolume"

   System.Func<string, GameObject> find = path =>
   {
       var slash = path.IndexOf('/');
       var rootName = slash < 0 ? path : path.Substring(0, slash);
       foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
       {
           if (root.name != rootName) continue;
           var t = slash < 0 ? root.transform : root.transform.Find(path.Substring(slash + 1));
           if (t != null) return t.gameObject;
       }
       return null;
   };

   var host = find(prefsPath);
   if (host == null) return "Host not found: " + prefsPath;

   var report = new System.Text.StringBuilder();
   var controls = new System.Collections.Generic.List<UnityEngine.Object>();
   foreach (var path in controlPaths)
   {
       var go = find(path);
       if (go == null) { report.AppendLine("SKIP not found: " + path); continue; }
       controls.Add(go);
   }

   foreach (var r in Yu5h1Lib.EditorExtension.PreferencesBindingUtility.BindSelected(host, controls))
       report.AppendLine(r.Outcome + " " + (r.Control != null ? r.Control.name : "null") + ": " + r.Message);
   return report.ToString();
   ```

5. **Save** the scene or prefab. Report every `SKIP`, `DuplicateName`, `AlreadyBound`, and `Unbindable` line to the user rather than working around it.

## Consuming the values

- Code: `PlayerPreferences.instance.current.TryGetValue("MasterVolume", out string raw)`, then parse with the adapter's format (plan § 可綁控件: bool is `"true"`/`"false"`, float is current-culture). Subscribe to `changed` for live updates. Prefer `TryGetValue`: the indexer throws on a missing key.
- No code: wire the control's own `onValueChanged` to the target in the Inspector. On load the stored value is written into the control, which fires `onValueChanged` only if it differs from the scene value. So a target that must be applied at startup also needs its initial state to match the control's scene value, or a code read at start. `Preferences` has no unconditional on-load notification yet (plan decision R1).

## Verify

Enter Play mode, change a control, exit, re-enter: the value should persist. Over MCP, drive the change with `execute_code` in Play mode (set `Slider.value`, `Toggle.isOn`, `InputField.text`; each fires `onValueChanged` and saves at once), then read the control back after re-entering. Exiting Play mode reverts the scene value but not the save, which is the point of the check. The key is shared by every scene using the same host type. In an application project whose saves matter, back up `PlayerPrefs.GetString("<KEY>")` first and restore it afterwards; in a test project such as Yu5h1LibTest, change it freely. Inspect storage with the host's context menu `PrintPlayerPrefs`, or `execute_code` returning `PlayerPrefs.GetString("<KEY>")` (`KEY` defaults to the host's type name). Reset with `ClearPlayerPrefs`.

## Known traps

Details and status in plan § 已知問題 / § 弊端評估.

- `DataViewBinding` loads saved values but does not write changes back: its value lives in another DataView with no change source (E4).
- A GameObject holding both `OptionSelector` and `OptionSet` has two `IValuePort` components; `ResolveBindableComponent`/`ResolvePort` bind whichever `GetComponents` returns first. Bind the intended one explicitly.
- Storage is JSON entries via `JsonUtility` (not hand-split anymore); a value with `,` or `"` round-trips correctly (E1/E2 fixed). An old save still on disk in the pre-migration flat format still loads fine too. Not yet fixed: a Slider saved on a comma-decimal locale can still fail `float.Parse` if reloaded under a different locale.
- A component that cannot bind (Dropdown, or `TMP_InputField` without `com.yu5h1.tmpextension`) is skipped; `BindAll` logs a Warning naming the object and its component type, and `PreferencesBindingUtility.BindSelected`/the `_bindings` drawer report it as `Unbindable` at bind time.
- Two GameObjects with the same name under one host share one field and overwrite each other; `BindAll` warns on the exact duplicate and logs a lighter hint when a field name looks like a Unity default (`Toggle`, `Slider (1)`, ...). The drawer and `BindSelected` catch it earlier, at bind time, but only for entries added through them.
- Controls instantiated after the host's `Awake` are not bound until `BindAll()` is called.
- Every change writes to disk; heavy Slider dragging is costly, especially on WebGL.
