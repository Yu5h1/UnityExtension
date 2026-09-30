# Preferences

Persist settings to PlayerPrefs with what Yu5h1Lib already ships. Load this before writing any save/load code for settings or control values: a control-edited value is wired in the scene over MCP, not coded.

Members, design decisions, known defects, weaknesses, and open decisions: [偏好設定](../../Documentation/偏好設定.md). Load it only when changing the system, hitting a trap listed below, or extending it. Code-level invariants for editing the binding source live in `yu5h1lib-conventions` (`csharp-core.md`).

## Choose the approach

| Situation | Use |
|---|---|
| A uGUI `Toggle`, `Slider`, `InputField`, `TMP_InputField`, `Dropdown`, `TMP_Dropdown`, or an `OptionSet` edits the value | `Preferences`: [scene procedure](#scene-procedure-mcp). `TMP_InputField`/`TMP_Dropdown` bind only when the project references `com.yu5h1.tmpextension` (`Packages/Plugins/TMP`); without it the control is skipped |
| An option switched by `OptionSelector` | Bind the `OptionSet` to save the item's value, or the `OptionSelector` to save the index. Only one per GameObject: both use the GameObject name as key |
| A UI Toolkit `Toggle`, `Slider`, `SliderInt`, `TextField`, or `DropdownField` (via `UIDocument`) edits the value | `PreferencesBinder`: [UI Toolkit scene procedure](#ui-toolkit-scene-procedure). Same `Preferences` host as uGUI. `EnumField` is not supported (skipped); route the gap to plan § UI Toolkit 適用分析 rather than hand-writing a port |
| Audio volume or mute | Bind the Sliders/Toggles with `Preferences` and wire each control's `onValueChanged` to `AudioMixerProxy` (`sound`/`bgm`/`voice`, linear 0–1 converted to dB). The mixer needs exposed parameters with those names |
| Code owns the value, no control edits it, no `changed`/`init` notification needed | Call `PlayerPrefs` directly (`JsonUtility` for complex types) |
| Code owns the value (any type), declared as a field on any class, wants `changed`/`init` events or a swappable serializer | `ObservablePref<T>` / `PlayerPrefValue<T>` (Core: `Unity/Core/Runtime/Source/ObservablePref.cs`, `PlayerPrefsValue.cs`; the copies under `Unity/UnityExtension/Runtime` are stale). A current layer, not a legacy one: field-owned, no scene host, any T. If a UI control edits the value, use `Preferences` instead |
| Password, token, or other secret | Not in PlayerPrefs: it is plaintext |
| A new uGUI control type must bind | Write one adapter (copy `ToggleAdapter`); `Preferences` needs no change |

## Keys

Every control bound to one host shares one save and one key space, whichever UI it belongs to.

- uGUI: the control's GameObject name. UI Toolkit: the control's `binding-path`.
- The same key under both UIs is the same saved value. When two UIs on one host edit different values, give one of them a distinct prefix (`UITK_MasterVolume`).
- Keys are a persistent contract: renaming a GameObject or `binding-path` orphans the saved value.
- An orphaned key stays in the save; nothing prunes it. `ClearPlayerPrefs` on the host resets the whole save.

## Scene procedure (MCP)

1. **Host.** Find an existing host with `find_gameobjects` by component `PlayerPreferences`, or a project subclass (grep the project for `: Preferences<`). If none, create an always-active GameObject and `manage_components add PlayerPreferences`. One host per Preferences type per scene. The host must be active at load: binding runs once in its `Start`, after every object's `Awake`, and `instance` on a missing host auto-creates an empty one with no bindings.
2. **Keys.** Rename each control with `manage_gameobject modify new_name` to a stable, meaningful name (`MusicEnabled`, `MasterVolume`), unique within the host, case-insensitive. See [Keys](#keys).
3. **Defaults.** Put authoritative defaults in the host's `defaultSetting` (a DataView: field name → string value, in the format listed in plan § 可綁控件). Set each control's scene value (`isOn`, `value`, `text`) with `manage_components set_property` only as the last fallback, and keep it equal to `defaultSetting` when both exist so the scene shows what a new player gets. Every field resolves "save → `defaultSetting` → control": a save's own value wins; a field the save lacks takes `defaultSetting` if it has one, else the control's current value. Loading a save also merges in any field `defaultSetting` has that the save lacks. Whatever fills a missing field is written to the save at once. Changing `defaultSetting` or a scene value later does not touch fields an existing save already has.
4. **Register.** In the Editor, select the target controls in the Hierarchy, then use the Preferences component's (or any Component's) gear-icon CONTEXT menu → **綁定選取的控件**; it only appears on a Component that has a `_bindings` field. That command, the `_bindings` drawer, and MCP all share one Editor API, `Yu5h1Lib.EditorExtension.PreferencesBindingUtility.BindSelected(host, controls)`: it accepts a control that is itself an `IValuePort`, else one the live adapter registry covers, and reports each control as `Bound`, `AlreadyBound`, `DuplicateName`, or `Unbindable`.

   The CONTEXT menu needs a `MenuCommand` to supply `context`, which MCP's `execute_menu_item` cannot provide — call the API directly with `execute_code`. Address objects by scene path: Unity 6.3 rejects `GetInstanceID()` and `EditorUtility.InstanceIDToObject(int)` as obsolete compile errors, and `Transform.Find` also reaches inactive children.

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

## UI Toolkit scene procedure

`Packages/UIToolkit/Runtime/Preferences/` (`com.yu5h1.uitoolkit`). `Preferences` itself has no idea UI Toolkit exists; a project can mix both UIs in one scene or across scenes.

1. **Host.** Reuse the existing `PlayerPreferences` (or project subclass) host from [scene procedure](#scene-procedure-mcp) step 1. Don't create a second one for UI Toolkit.
2. **Panel.** A `UIDocument` needs `visualTreeAsset` and `panelSettings`. A `PanelSettings` created with `ScriptableObject.CreateInstance` has no theme: assign a `ThemeStyleSheet` (a project's `UnityDefaultRuntimeTheme.tss`, or one made with *Create > UI Toolkit > Default Runtime Theme File*), or the controls render unstyled. To match a Canvas using *Constant Pixel Size*, set `scaleMode = ConstantPixelSize`, `scale = 1`.
3. **Binder.** Add `Yu5h1Lib.UIToolkit.PreferencesBinder` to the `UIDocument`'s GameObject (it has `[RequireComponent(typeof(UIDocument))]`). Assign its `_preferences` field to the host; the field is restricted to `IPreferences` via `[TypeRestriction]`.
4. **Keys.** Set each control's `binding-path` in the `.uxml` source (`<ui:Toggle binding-path="sound" .../>`); see [Keys](#keys). A control with no `binding-path`, or one that isn't `IBindable`, is skipped. There is no batch-bind step: editing the UXML is the whole registration.
5. **Supported controls.** `Toggle`, `Slider`, `SliderInt`, `TextField`, `DropdownField` (`VisualElementPortFactory`). Each writes the same format as its uGUI counterpart (plan § 需先驗證 has the correspondence table), so one save round-trips through either UI.
6. **Defaults.** The host's `defaultSetting`, same as [scene procedure](#scene-procedure-mcp) step 3.
7. **Save** the scene or prefab.

A `UIDocument` root ignores picking, so a UI Toolkit panel beside a uGUI Canvas does not block the Canvas's clicks.

## Consuming the values

- Code: `PlayerPreferences.instance.current.TryGetValue("MasterVolume", out string raw)`, then parse in the format plan § 可綁控件 lists: bool is `"true"`/`"false"`; float and int are `InvariantCulture` — parse with `InvariantCulture`, falling back to `CurrentCulture` for old comma-decimal saves. Subscribe to `changed` for live updates. Prefer `TryGetValue`: the indexer throws on a missing key.
- No code: wire the control's own `onValueChanged` to the target in the Inspector. `BindAll`/`BindPort` notify unconditionally right after loading, so the target's listener runs once at bind time even when the loaded value equals the control's scene value; nothing extra is needed to apply a saved value at startup.

## Verify

Enter Play mode, change a control, exit, re-enter: the value should persist. Over MCP, drive the change with `execute_code` in Play mode (set `Slider.value`, `Toggle.isOn`, `InputField.text`, or a UI Toolkit control's `value`/`index`; each fires its change event and saves at once), then read the control back after re-entering. Exiting Play mode reverts the scene value but not the save, which is the point of the check. The key is shared by every scene using the same host type. In an application project whose saves matter, back up `PlayerPrefs.GetString("<KEY>")` first and restore it afterwards; in a test project such as Yu5h1LibTest, change it freely. Inspect storage with the host's context menu `PrintPlayerPrefs`, or `execute_code` returning `PlayerPrefs.GetString("<KEY>")` (`KEY` defaults to the host's type name). Reset with `ClearPlayerPrefs`.

## Known traps

Details and status in plan § 已知問題 / § 弊端評估.

- `DataViewBinding` loads saved values but does not write changes back: its value lives in another DataView with no change source (E4).
- A GameObject holding both `OptionSelector` and `OptionSet` has two `IValuePort` components; `ResolveBindableComponent`/`ResolvePort` bind whichever `GetComponents` returns first. Bind the intended one explicitly.
- Storage is `{"_entries":[{"key":...,"value":...}]}` via `JsonUtility`; values containing `,` or `"` round-trip. Saves in the older flat format still load.
- A control that cannot bind (`Scrollbar`, `EnumField`, or `TMP_InputField`/`TMP_Dropdown` without `com.yu5h1.tmpextension`) is skipped; `BindAll` logs a Warning naming the object and its component type, and `BindSelected`/the `_bindings` drawer report it as `Unbindable` at bind time.
- Two controls with the same field name under one host share one field and overwrite each other, whether both are uGUI, both UI Toolkit, or one of each. `BindPort` (which `BindAll` and `PreferencesBinder` both go through) warns at runtime but still binds both; `BindAll` also logs a lighter hint when a uGUI name looks like a Unity default (`Toggle`, `Slider (1)`, ...). The drawer and `BindSelected` catch uGUI duplicates earlier in the Editor, but only for entries added through them; for UI Toolkit, the `PreferencesBinder` Inspector lists every `binding-path` its UXML produces, outlines the ones colliding with this UXML, another binder on the same host, or the host's uGUI `_bindings`, and renames one by rewriting just that attribute in the `.uxml` (a rename orphans the saved value under the old key, and is not undoable).
- Code that binds a port through `BindPort` must release it through `UnbindPort`, not `port.Unbind()`: the host keeps a per-field registry for the duplicate check, and a port released behind its back makes the next rebind report a false conflict.
- uGUI controls instantiated after the host's `Start` are not bound until `BindAll()` is called.
- Every change writes to disk; heavy Slider dragging is costly, especially on WebGL.
- With `PlayerPrefJsonSerializer` swapped in, `PlayerPrefValue<T>`/`ObservablePref<T>` of an array or collection type throws `NotSupportedException`: `JsonUtility` cannot store one at top level. Wrap it in a `[Serializable]` class. Primitives such as `double` never reach the object serializer.
- `PreferencesBinder` rebinds on every `OnEnable`, unlike the uGUI host's single `Start`: `UIDocument` rebuilds `rootVisualElement` each time it is re-enabled. A script that assumes UI Toolkit controls were bound once at startup is wrong.
