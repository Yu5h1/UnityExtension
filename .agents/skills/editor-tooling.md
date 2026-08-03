# Unity Editor tooling

Use this skill for Yu5h1Lib Inspector, PropertyDrawer, EditorWindow, context-menu, shortcut, AssetDatabase, sub-asset, and other Editor extension work.

## A Header must say something no field name says

The first field of a group already labels it. A `[Header]` earns its place only
when it names a concept none of the fields carry.

```csharp
[Header("Speed Limit")]     // says nothing the field does not
public float speedLimit;

[Header("Physics")]         // no field is called physics; this groups
public float mass;
public float jointCompliance;
public float jointDamping;
```

Drop it when the header and the first field are the same word, and when every
field in the group already carries the term — `[Header("Initial Spawn")]` over
`initialCount`, `initialVelocity` and `spawnVolume` is three restatements.

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
