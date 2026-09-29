# Unity agent knowledge

This directory is the canonical AI-agent knowledge entry for UnityExtension and reusable Yu5h1Lib Unity development:

`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension\.agents`

Scope: **everything under `Unity/UnityExtension/`, including every package**, whether or not that package keeps its own `handoff.md`.

## House conventions

Read before writing any Unity code in this tree. These two get written wrong most often, and each one costs a manual correction.

- **Never write `[CreateAssetMenu]`.** Yu5h1Lib already handles ScriptableObject creation, driven by the field that references the object.
- **Put `[Inline]` on every serialized ScriptableObject reference field.**
- **The root namespace is `Yu5h1Lib`. There is no `Yu5h1` namespace.** A package nests under it (`Yu5h1Lib.UnifiedSolver`, `Yu5h1Lib.ParticlePhysics`). Never name a namespace after a type that lives in it, and never take a name `UnityEngine` already uses for a type — an enclosing namespace beats a `using`, so `Yu5h1Lib.Physics` would shadow `UnityEngine.Physics` in every file in the library.
- **A custom inspector derives from `Editor<T>`,** never from `UnityEditor.Editor`. It hands you a cast `targetObject` and a full default inspector; see [editor-tooling.md](skills/editor-tooling.md).
- **A setting is for a real choice.** If code can settle it, code settles it — never add a field, a toggle, or a `[RequireComponent]` for something already determined by other data. See [editor-tooling.md](skills/editor-tooling.md).

Reasoning for the first two is in [data-architecture.md](skills/data-architecture.md).

## Core source and backups

- **Core source lives outside this tree**, in the `Unity/Core` repository: `Unity/Core/Runtime/Source` and `Unity/Core/Editor/Source`. Building `Unity/Core/Yu5h1LibForUnity.sln` produces `Packages/Core/Runtime/Yu5h1Lib.Runtime.dll` and `Packages/Core/Editor/Yu5h1Lib.Editor.dll`, and those DLLs are what every package here compiles against. A Core change reaches Unity only after that build. Changing Core needs user authorization; see the shared `yu5h1lib-conventions` skill, `references/csharp-core.md`.
- **`Unity/UnityExtension/Runtime/` and `Unity/UnityExtension/Editor/` are backup copies.** They hold same-named types (`EditorScopes`, `EditorAdvanced`, `ReorderableListEnhanced`, `TypeRestrictionAttribute`, …) but are not what ships. Never read them as the source, never edit them, and never keep them in sync with Core. When a type exists in both places, the `Unity/Core` copy is the one to read and change.
- Removing the backups is tracked in [handoff.md § Next Steps](../handoff.md#next-steps), the backup-cleanup item.

## Entry points

- Current UnityExtension state and next steps: [handoff.md](../handoff.md).
- A tracked task ID resolves to the file in this project that declares `Task ID: <id>`; see `AgentArtifactGuide.md` § Task identity.
- Viewer-facing progress: [report.json](../report.json) with optional [report.dev.json](../report.dev.json) developer details.
- Architecture and refactor design records: [plans/](plans/).
- Application validation of reusable capabilities: BonghuoVR, entry `W:/UnityProject/BonghuoVR/handoff.md` (TaskProgress scope `bonghuo-vr`).
- Reusable Unity techniques: [skills/](skills/), loading only the file that matches the task.

## Structure

- [skills/](skills/) — reusable techniques, separated by development scope.
- [plans/](plans/) — architecture and refactor design records.

## Skills

Read only what matches the current task:

- [data-architecture.md](skills/data-architecture.md) — ScriptableObject architecture, Parameter/Member/Invocation objects, ValuePort, Theme, and presets. Load its linked architecture plan only for design or Timeline integration changes.
- [editor-tooling.md](skills/editor-tooling.md) — Inspector and Editor extensions, SubAssets, Undo, shortcuts, and internal Unity APIs.
- [unity-event-extensions.md](skills/unity-event-extensions.md) — ParameterObject arguments, dynamic persistent arguments, MessageSender/Receiver, and UnityEvent extension techniques. It routes to the closed Invocation/Transmission design records when history is needed.
- [preferences.md](skills/preferences.md) — persisting settings with PlayerPrefs: what to use for UI-edited versus code-owned values, and wiring `Preferences` in the scene over MCP instead of writing save/load code.
- [unity-serialization.md](skills/unity-serialization.md) — `Optional<T>`, `SerializedType`, `SerializedAssembly`, and Unity serialization boundaries.
- [gpu-rendering.md](skills/gpu-rendering.md) — procedural instancing, ComputeBuffer ownership, shader keyword variants, vertex channel payloads, struct layout shared across C#/compute/shader, and reading state out of a vendored solver.
- [unified-solver.md](skills/unified-solver.md) — assembling `Packages/Unified-Solver`: which components pair with which profiles, water and locomotion setup, the velocity-versus-position channels, mesh conventions, and what to check when something silently does nothing. Its live state is in that package's `handoff.md` and its reasoning in `plan.md`; this skill is neither.
- [Unity CLI](skills/unity-cli/SKILL.md) — Unity's official CLI and `com.unity.pipeline`: what they add over the `unityMCP` bridge already in use, version requirements, and beta rough edges, for anyone deciding whether to adopt or troubleshoot them.

For a tracked task, open the file that declares its ID. Use [handoff.md](../handoff.md) for live coordination state and the reports for progress summaries.

Keep this file as a concise introduction. Put procedures and technical details in the matching skill.
