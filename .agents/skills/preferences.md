# Preferences

Persist settings to PlayerPrefs with what Yu5h1Lib already ships. Load this before writing any save/load code for settings or control values: a uGUI-edited value is wired in the scene over MCP, not coded.

Members, design decisions, known defects, weaknesses, and open decisions: [偏好設定](../../Documentation/偏好設定.md). Load it only when changing the system, hitting a trap listed below, or extending it. Code-level invariants for editing the binding source live in `yu5h1lib-conventions` (`csharp-core.md`).

## Choose the tier

The two tiers store differently and do not share keys. Pick one per value; do not persist the same setting through both.

| Situation | Use |
|---|---|
| A uGUI `Toggle`, `Slider`, `InputField`, `TMP_InputField`, or an `OptionSet` edits the value | UI binding: [scene procedure](#scene-procedure-mcp) below |
| Code owns the value (no control edits it), needs a type and a change event | `[SerializeField] ObservablePref<T>` with `key` + `defaultValue`; call `Init()` at startup. Wire `init`/`changed` in the Inspector to apply it without code |
| One setting shared by several scenes | `PlayerPrefObject<T>` asset (e.g. `PlayerPrefBoolObject`) referenced from each scene |
| BGM／SFX／voice volume and a BGM on/off switch | Existing `AudioVolumePrefs` component; read its static properties |
| `Dropdown` / `TMP_Dropdown` | Not bindable yet (silently ignored). Plan decision D3 |
| UI Toolkit control | Not supported. Report the gap and route to plan § UI Toolkit 適用分析; do not hand-write a replacement |
| Password, token, or other secret | Neither tier: PlayerPrefs is plaintext |
| A new uGUI control type must bind | Write one adapter (copy `ToggleAdapter`); `Preferences` needs no change |

## Scene procedure (MCP)

1. **Host.** Find an existing host with `find_gameobjects` by component `PlayerPreferences`, or a project subclass (grep the project for `: Preferences<`). If none, create an always-active GameObject and `manage_components add PlayerPreferences`. One host per Preferences type per scene. The host must be active at load: binding runs once in its `Awake`, and `instance` on a missing host auto-creates an empty one with no bindings.
2. **Keys.** Each control's GameObject name is its saved key. Rename with `manage_gameobject modify new_name` to a stable, meaningful name (`MusicEnabled`, `MasterVolume`), unique within the host, case-insensitive. Treat names as a persistent contract: a later rename orphans saved values.
3. **Defaults.** Set each control's scene value (`isOn`, `value`, `text`) with `manage_components set_property`. On first run a missing key takes the control's current value. The host's `defaultSetting` overrides that only when no save exists at all.
4. **Register.** Fill in the ids and run with `execute_code`. It accepts a component that is itself an `IValuePort`, else one the live adapter registry covers, so it never needs a separately maintained list of bindable types.

   ```csharp
   int prefsObjectId = 0;            // GameObject holding the Preferences component
   int[] controlObjectIds = { };     // GameObjects holding the controls

   var bindable = new System.Collections.Generic.List<System.Type>();
   foreach (var kv in Yu5h1Lib.AdapterFactory<Component>.Adapters)
       if (typeof(Yu5h1Lib.MVVM.IValuePort).IsAssignableFrom(kv.Key))
           bindable.AddRange(kv.Value);

   var prefsGo = (GameObject)EditorUtility.InstanceIDToObject(prefsObjectId);
   SerializedObject so = null;
   foreach (var m in prefsGo.GetComponents<MonoBehaviour>())
   {
       var probe = new SerializedObject(m);
       if (probe.FindProperty("_bindings") != null) { so = probe; break; }
   }
   if (so == null) return "No Preferences component on " + prefsGo.name;
   var list = so.FindProperty("_bindings");

   var keys = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
   for (int i = 0; i < list.arraySize; i++)
   {
       var bound = list.GetArrayElementAtIndex(i).objectReferenceValue as Component;
       if (bound != null) keys.Add(bound.gameObject.name);
   }

   var report = new System.Text.StringBuilder();
   if (bindable.Count == 0) report.AppendLine("WARN adapter registry empty: is com.yu5h1.ui installed and compiled?");
   foreach (var id in controlObjectIds)
   {
       var go = (GameObject)EditorUtility.InstanceIDToObject(id);
       Component target = null;
       foreach (var c in go.GetComponents<Component>())
           if (target == null && c is Yu5h1Lib.MVVM.IValuePort) target = c;
       if (target == null)
           foreach (var c in go.GetComponents<Component>())
               foreach (var t in bindable)
                   if (target == null && t.IsInstanceOfType(c)) target = c;
       if (target == null) { report.AppendLine("SKIP not bindable: " + go.name); continue; }
       if (!keys.Add(go.name)) { report.AppendLine("SKIP key already bound: " + go.name); continue; }
       list.arraySize++;
       list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = target;
       report.AppendLine("BOUND " + go.name + " -> " + target.GetType().Name);
   }
   so.ApplyModifiedProperties();
   return report.ToString();
   ```

5. **Save** the scene or prefab. Report every `SKIP` line to the user rather than working around it.

## Consuming the values

- Code: `PlayerPreferences.instance.current.TryGetValue("MasterVolume", out string raw)`, then parse with the adapter's format (plan § 可綁控件: bool is `"true"`/`"false"`, float is current-culture). Subscribe to `changed` for live updates. Prefer `TryGetValue`: the indexer throws on a missing key.
- No code: wire the control's own `onValueChanged` to the target in the Inspector. On load the stored value is written into the control, which fires `onValueChanged` only if it differs from the scene value. So a target that must be applied at startup also needs its initial state to match the control's scene value, or a code read at start. If that is not acceptable, the value belongs to the `ObservablePref` tier, whose `init` event fires unconditionally.

## Verify

Enter Play mode, change a control, exit, re-enter: the value should persist. Inspect storage with the host's context menu `PrintPlayerPrefs`, or `execute_code` returning `PlayerPrefs.GetString("<KEY>")` (`KEY` defaults to the host's type name). Reset with `ClearPlayerPrefs`.

## Known traps

Details and status in plan § 已知問題 / § 弊端評估.

- `ValuePort` and `OptionSet` load saved values but do not write changes back (E4).
- InputField text containing `,` or `"`, and Slider values in comma-decimal locales, may corrupt on reload (E1/E2).
- The first run always logs `Failed to parse preferences`; this is expected, not an error (E3).
- Controls instantiated after the host's `Awake` are not bound until `BindAll()` is called.
- Every change writes to disk; heavy Slider dragging is costly, especially on WebGL.
