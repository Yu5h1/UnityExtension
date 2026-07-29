# Unity Editor tooling

Use this skill for Yu5h1Lib Inspector, PropertyDrawer, EditorWindow, context-menu, shortcut, AssetDatabase, sub-asset, and other Editor extension work.

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
