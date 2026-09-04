# Unity Editor tooling

Use this skill for Yu5h1Lib Inspector, PropertyDrawer, EditorWindow, context-menu, shortcut, AssetDatabase, sub-asset, and other Editor extension work.

## Derive from `Editor<T>`, never from `UnityEditor.Editor`

Every custom inspector derives from `Yu5h1Lib.EditorExtension.Editor<TargetType>`
(`Unity/UnityExtension/Editor/Source/EditorAdvanced.cs`). It ships inside the
precompiled `Packages/Core/Editor/Yu5h1Lib.Editor.dll`, which every asmdef
auto-references, so there is nothing to add to `references` — just
`using Yu5h1Lib.EditorExtension;`. `Editor<T>` and `UnityEditor.Editor` differ in
arity, so both `using` directives can coexist without ambiguity.

What the base class already does, and therefore what not to hand-write:

- `targetObject` and `targetObjects` are already cast to `TargetType`. Never
  write `(Foo)target` or `targets.Cast<Foo>()`.
- The default `OnInspectorGUI` runs `this.Iterate(DrawProperty, DrawMonoScript)`:
  it walks the whole serialized object, draws the `m_Script` row disabled, wraps
  every array in a `ReorderableListEnhanced`, and applies modified properties
  inside one change check. An inspector that only needs one field drawn
  differently overrides `DrawProperty`, not `OnInspectorGUI`.
- `TryPrepareList(property, out var list)` yields that same reorderable list for
  a property drawn by hand, cached per property path.

Three traps:

- **`Iterate` never calls `serializedObject.Update()`**, unlike Unity's own
  `DrawDefaultInspector`. Nothing outside the inspector — `OnSceneGUI`, an Undo,
  another editor — shows up until something else updates the serialized object.
  An editor that edits its target anywhere but the inspector calls
  `serializedObject.Update()` itself before `base.OnInspectorGUI()`.
- `EditorAdvanced.OnDisable` is `protected virtual` and clears the cached lists.
  A plain `void OnDisable()` in the derived class hides it — a CS0114 warning and
  stale lists. Write `protected override void OnDisable()` and call
  `base.OnDisable()`.
- `EnteredPlayMode`, `ExitingPlayMode`, `EnteredEditMode`, `ExitingEditMode` and
  `HierarchyChanged` are **not wired by default**. They reach an editor only
  after `EditorAdvanced.RegisterAdvancedMethods(this)`, and only while the
  `UseAdvancedEvents` EditorPref is on — it defaults to `false`, and no editor in
  the library registers today. Treat them as opt-in, not as events that fire.

## Separate with `[Space]`; add `[Header]` only when it says something new

Groups need separating, not always labelling. Use `[Space]` for the gap, and
promote it to `[Header]` only when the name is something no field in the group
already carries.

```csharp
[Space]                 // speedLimit already labels the group
public float speedLimit;
public float speedDecayRate;

[Header("Physics")]     // no field is called physics
public float mass;
public float jointCompliance;
public float jointDamping;
```

## Tooltips are one line, and usually absent

The field name carries the meaning. A tooltip is for the one thing the name
cannot hold — a unit, a disabling value, a non-obvious limit — and nothing else.

- **Default to no tooltip.** `sleepDelay`, `weight4`, `castShadows` explain
  themselves; a sentence restating the name is noise in the inspector and noise
  in the source.
- **One line when there is one.** `"m/s. 0 disables sleeping."` is a good
  tooltip. If it runs past a line, the content belongs somewhere else.
- **A tooltip that needs more than one source line must use a verbatim string,**
  `[Tooltip(@"...")]`. An ordinary C# string literal cannot span source lines and
  will not compile. Inside `@"..."` there are no escapes and a quote is written
  `""`. Reaching for this is the signal that the text is too long, not a licence
  to keep going.
- **Reasoning goes above the field as a `//` comment, or in `<summary>`.** That
  is where the *why* is useful — to whoever edits the code next, and to an agent
  reading it — and it costs the inspector nothing. Note that
  `.agents/CodingConventions.md` bans explanatory comments *inside method
  bodies*; a comment above a field or a type is not that.
- Rename the field before writing a tooltip to explain it. `speedLimit` plus
  `"m/s. 0 disables."` beats `limit` plus a sentence.

## A setting is for a real choice

If code can settle something, code settles it. A serialized field is for a
decision only a human can make, never for restating something already determined
elsewhere in the data.

- A field whose only correct value is derivable from other fields must be
  derived, not authored. Two places holding the same fact means they can
  disagree, and the usual symptom is nothing happening with no error.
- Companion components are the same fault in another form. `[RequireComponent]`
  does not solve "this needs that": it converts *forgot to add it* into *forced
  to look at it*, which is still manual work and still inspector noise.
- The owning component should add its companions itself, from `Reset` for the
  edit-time path and `Awake` for the runtime one, mark them
  `HideFlags.HideInInspector`, and draw them as foldout modules from its own
  custom editor. This is exactly how Unity ships ParticleSystem:
  `ParticleSystemRenderer` is a genuine second component that users never
  experience as one.
- Add companions unconditionally rather than gating on what the current
  configuration appears to need. A gate leaves the object one component short as
  soon as the configuration changes, and says nothing.
- **`[RequireComponent]` only pulls in the opposite direction from the one you
  want.** Put it on the companion and it pulls in the owner — never the reverse.
  So an owner that "requires" a companion by declaring the attribute on the
  companion gets nothing, and the failure is silent: a fully configured object
  that runs and does nothing, which is indistinguishable from a broken feature.
  Check the attribute's direction before assuming a companion is reachable.
- Two costs to cover before hiding anything: the owner must remove its
  companions when it is itself removed, or they become components that cannot be
  seen and therefore cannot be deleted; and a companion must not declare
  `[RequireComponent]` back at its owner, or removing the owner raises a dialog
  naming a component the user cannot see. Defer the cleanup through
  `EditorApplication.delayCall`, guarded on the GameObject still being alive, so
  scene close and play-mode exit do not trip it.
- Defaults must produce something visible. A component that does nothing until
  configured looks broken, and hides every other fault behind the same blank
  screen.

## Read-only Log panel

For an embedded IMGUI log, use `ReadOnlyLogSection`: an external foldout title above a framed
section, a fixed toolbar, scrolling messages and a draggable bottom edge. It delegates text display
to `ReadOnlyLogPanel`, which remains available for hosts that supply their own chrome or Rect.

- Package: `com.yu5h1.common`; Editor assembly: `Yu5h1Lib.Common.Editor`.
- Namespace: `Yu5h1Lib.EditorExtension`.
- Sources: [ReadOnlyLogSection.cs](../../Packages/common/Editor/Tool/ReadOnlyLogSection.cs),
  [ReadOnlyLogPanel.cs](../../Packages/common/Editor/Tool/ReadOnlyLogPanel.cs).
- Section API: `new ReadOnlyLogSection(float height = 160)` and
  `bool DrawLayout(string title, Func<string> getText, Action drawToolbar)`.
  Height refers to the message viewport, excluding title, toolbar and bottom grip.
- The whole section uses 4 GUI points of padding on each side within the current layout and
  follows the caller's `EditorGUI.indentLevel`. Indentation is applied once to title and frame;
  the caller's indent is restored after drawing. No window-width calculation is needed.
- Retain one section per visible log. `Expanded`, `Height`, `FontSize` and `SearchText` expose its display state.
  Foldout starts open; dragging the bottom edge changes height between `MinimumHeight` (60) and
  `MaximumHeight` (800) GUI points. Collapsing preserves height, font and scroll position.
- `drawToolbar` runs inside the native horizontal toolbar scope. Use `section.DrawToolbarButton("Clear")`
  first, then append future actions to its right. Buttons use the native flat toolbar style;
  an explicit top border completes the embedded frame. The built-in native search field sits on
  the right, using available width up to 220 GUI points. Controls stay outside the message viewport.
- Search displays complete lines containing the literal query, ignoring case. It preserves original
  line endings; an empty query restores the source text. Search changes reset the viewport, and
  collapsing preserves the query. Filtering does not change caller data: Clear still clears the
  caller's entire log. Search state belongs to each section instance.
- `getText` runs after toolbar actions so Clear or Append affects the displayed text immediately.
  Callbacks own the source text, ordering, timestamps, capacity and clear/save behavior.
- Draw returns whether display state changed. Call the host's `Repaint()` when true.
  Recreating the instance in `OnGUI` resets its state. State is not persisted across domain reloads.

Put the caller under an Editor-only assembly. An asmdef-based caller explicitly adds
`"Yu5h1Lib.Common.Editor"` to `references`; keep this reference out of Runtime asmdefs.

```csharp
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.EditorExtension;

public sealed class DiagnosticLogWindow : EditorWindow
{
    private readonly ReadOnlyLogSection log = new ReadOnlyLogSection();
    private string text = "第一行\nSecond line\n";

    [MenuItem("Window/Diagnostic Log")]
    private static void Open() => GetWindow<DiagnosticLogWindow>();

    private void OnGUI()
    {
        if (log.DrawLayout("Log", () => text, DrawToolbar))
            Repaint();
    }

    private void DrawToolbar()
    {
        if (log.DrawToolbarButton("Clear"))
            text = string.Empty;
    }
}
```

For a custom Inspector, retain the same field on `Editor<TargetType>` and make the same draw/repaint
call inside `OnInspectorGUI`; call `base.OnInspectorGUI()` when the default fields are wanted.
The section draws its own frame and foldout: avoid an additional help-box wrapper around it.

For a bare message viewport use `ReadOnlyLogPanel.Draw(Rect position, string text)` or
`ReadOnlyLogPanel.DrawLayout(string text, float height)`. These return whether the font changed.
The panel accepts null as empty and treats markup literally. Do not use a disabled scope to make
it read-only: native `SelectableLabel` already prevents editing while retaining selection/copy.

The wheel scrolls normally. Ctrl+wheel changes font one point per vertical wheel event;
Ctrl+middle-click restores `ReadOnlyLogPanel.DefaultFontSize` (12 pt). Font limits are exposed by
`MinimumFontSize` (8) and `MaximumFontSize` (32). Font-control events inside the visible message
viewport (including its scrollbar) are consumed even at the limits and do not also scroll it or its
parent. Title, toolbar and resize grip are outside that viewport. Width and font changes recalculate
wrapping and text height. Foldout, resizing and font changes do not set `GUI.changed` themselves.

Ancestor clipping is read through one cached delegate to internal `UnityEngine.GUIClip.visibleRect`;
Unity has no equivalent public query. If it cannot bind, font gestures and starting a resize are
disabled; normal wheel scrolling, selection and toolbar/foldout rendering use public APIs.
Check binding when adopting a new Unity version instead of dropping the clipping check.

Use an existing consuming EditorWindow or Inspector for interaction verification. Check
collapse/reopen, resize limits, release outside the grip, selection/copy and scrolling. For
independence and clipping checks, use two instances in an existing UI viewer or a temporary test
host; the minimal usage above supplies the entry point. Keep acceptance results in the consumer's
verification record.

## Native-first workflow

Before designing or implementing an extension:

1. Inspect the relevant Unity window, Inspector, track, component, context menu, and Shortcuts Manager.
2. Check whether Unity already exposes the operation through an existing field, command, menu item, or standard workflow.
3. Check the public Editor API and the installed package version.
4. Tell the user what Unity already supports and what is actually missing.
5. Extend Unity only when the native behavior does not satisfy the requested workflow.

Do not create a convenience command that merely duplicates a discoverable native control unless the user explicitly wants automation, batching, synchronization, or a different UX.

## Implementation boundaries

- Prefer public APIs and package-defined actions.
- Inspect the installed Unity/package assembly or source before relying on internal types; online examples may target an older package version.
- Use reflection only for a confirmed capability gap. Isolate it, tolerate missing types/members, and preserve a safe no-op path.
- Give shortcuts the narrowest applicable context. A context-specific shortcut may share a key with a global command without becoming an equal-scope conflict.
- Preserve Undo for user-visible asset and serialized-object changes.
- Mark modified objects dirty only when needed; save assets deliberately rather than as an incidental side effect.

## Existing helpers

- `Unity/UnityExtension/Editor/Source/EditorAdvanced.cs` — `EditorAdvanced` and
  `Editor<TargetType>`, the base class every inspector uses. See the first
  section.
- `Packages/common/Editor/Utility/SubAssetUtility.cs` — main/sub-asset creation, lookup, and removal.
- `Packages/common/Editor/Utility/ParameterObjectUtility.cs` — resolves concrete ParameterObject implementations.
- `Runtime/Utility/StringOptionsProvider.cs` — option-provider registry used by Editor drawers.
- `Packages/common/Editor/` — Inspectors, PropertyDrawers, extensions, and Editor utilities.

## Sub-asset editors

- Add sub-assets to the actual main asset; sub-assets cannot contain nested sub-assets.
- On removal, update serialized references and destroy owned sub-assets in one Undo-aware operation.
- Use object references as identity; use names for human readability, not ownership.
- Route runtime data-model decisions to [data-architecture.md](data-architecture.md).

## Option providers

- Reuse `StringOptionsProvider` for stable Inspector option sources.
- Keep registration close to the Editor feature that owns the options.
- Use display formatting separately from stored values when serialized identifiers must remain stable.
