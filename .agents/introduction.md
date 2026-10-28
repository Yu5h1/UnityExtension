# Unity agent knowledge

This directory is the canonical AI-agent knowledge entry for UnityExtension and reusable Yu5h1Lib Unity development:

`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension\.agents`

Scope: **all Unity development, packages, samples, and consumer projects using Yu5h1Lib Unity technologies** (including `Unity/**`, `UnityExtension/**`, and host/test projects), whether or not a package keeps its own `handoff.md`.

## House conventions

Read before writing any Unity code in this tree or using Yu5h1Lib Unity technologies. These get written wrong most often, and each one costs a manual correction.

- **Never write `[CreateAssetMenu]`.** Yu5h1Lib already handles ScriptableObject creation, driven by the field that references the object.
- **Put `[Inline]` on every serialized ScriptableObject reference field.**
- **The root namespace is `Yu5h1Lib`. There is no `Yu5h1` namespace.** A package nests under it (`Yu5h1Lib.UnifiedSolver`, `Yu5h1Lib.ParticlePhysics`). Never name a namespace after a type that lives in it, and never take a name `UnityEngine` already uses for a type — an enclosing namespace beats a `using`, so `Yu5h1Lib.Physics` would shadow `UnityEngine.Physics` in every file in the library.
- **A custom inspector derives from `Editor<T>`,** never from `UnityEditor.Editor`. It hands you a cast `targetObject` and a full default inspector; see [editor-tooling.md](skills/editor-tooling.md).
- **A setting is for a real choice.** If code can settle it, code settles it — never add a field, a toggle, or a `[RequireComponent]` for something already determined by other data. See [editor-tooling.md](skills/editor-tooling.md).
- **Call the Yu5h1Lib wrapper, never the engine API it wraps.** Procedure and the known wrappers: [Engine API wrappers](#engine-api-wrappers).

Reasoning for the first two is in [data-architecture.md](skills/data-architecture.md).

## Engine API wrappers

Unity renames and deprecates APIs between versions, and Core ships as one precompiled DLL that must load on both supported editors (6000.3 and 6000.6). Yu5h1Lib wraps the volatile engine APIs so each change is absorbed in one place; code that calls the engine API directly breaks again at the next rename. This applies in packages and consumer projects alike. Vendored third-party code (for example `Assets/Plugins/<vendor>/**`) stays as shipped.

1. **Before calling a `UnityEngine` or `UnityEditor` API, look for its wrapper.** Search `Unity/Core/Runtime/Source`, `Unity/Core/Editor/Source` and `Packages/common` for the engine API's name; a Yu5h1Lib method that calls it is the wrapper. Unity Core ships no IntelliSense XML, so source search is the discovery route. The table below lists the wrappers already known.
2. **Keep the call's behavior when switching to the wrapper.** Wrapper defaults can differ from the engine's; the table names those that do.
3. **When the wrapper lacks what you need, compose it from the wrapper** (`ObjectUtility.FindObjects(type).FirstOrDefault()` for a single object by `Type`) **or ask the user for a Core addition.** Do not fall back to the engine API. An assembly with `overrideReferences` needs `Yu5h1Lib.Runtime.dll` in `precompiledReferences` to see Core.
4. **Before reporting, read your diff for engine calls** that step 1 would have redirected.

| Engine API | Wrapper | Behavior to keep |
|---|---|---|
| Scene queries: `Object.FindObjectsOfType`, `FindObjectOfType`, `FindObjectsByType`, `FindAnyObjectByType`, `FindFirstObjectByType` | `ObjectUtility.FindObject<T>`, `FindObjects<T>`, `FindObjects(Type)`, `TryFind<T>` (`Unity/Core/Runtime/Source/Utility/ObjectUtility.cs`) | `includeInactive` defaults to `true`; the engine's no-argument overloads exclude inactive objects, so pass `false` where the original call had no `FindObjectsInactive.Include`. Results are unordered. |
| `UnityEngine.Input` | `Yu5h1Lib.InputHandler` (`Packages/common/Runtime/Input/InputHandler.cs`), over Legacy Input (`LegacyInputBackend`) or the Input System package (`InputSystemBackend`, `Packages/InputSystem`), whichever Player Settings' `Active Input Handling` allows | For UGUI, attach `Yu5h1Lib.UI.UIInputHandler` (a `BaseInput` bridge to `InputHandler`) as the `StandaloneInputModule`'s `inputOverride`, so the module never falls back to `UnityEngine.Input`. |

Add a row when Core gains a wrapper; changing Core itself needs the user's authorization (`yu5h1lib-conventions`, `references/csharp-core.md`).

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

## Adopting a UX module in a package

The workflow (inspect, implement the gaps, accept) is the shared [spec-adoption](../../../.agents/skills/spec-adoption/SKILL.md) skill — the only adoption skill (UX decision 82; the former `web-replication` skill is deleted). Verification-only requests use [spec-acceptance](../../../.agents/skills/spec-acceptance/SKILL.md), which routes to that shared workflow. This section keeps only what is Unity-specific.

- **Sources.** Behavior comes from the module's UX spec, `UX/Documentation/ModuleSpec/[<layer>/]<Module>.md`, and nothing else: never derive behavior from old Unity samples or from Web source when the spec is silent — raise the gap in `Documentation/Requirements.UX.md` and wait. Theme (`Web/dist/theme/theme.css`) and config declarations stay Web deliveries with their manifests until UX takes them over (gaps go to `Requirements.Web.md`). Web tests passing never counts as Unity verification. Unity-side choices live in `decisions/cross-platform-ui-parity.decisions`.
- **Pinning.** Snapshot the spec, design data and cases byte-identically into `Packages/<package>/Tests/Editor/UX~/<Module>/` (`.gitattributes` `-text`), and let an EditMode `<Module>DesignTests` check the SHA-256 against the UX manifest and the design values. Refuse an unknown major version or a dirty delivery unless the user accepts it. The Yu5h1Lib.UIToolkit Editor importer is for Theme and config only (USS variables, WebTheme, imported-version stamp; re-import of the same input produces no diff).
- **Implementing.** Native first (UX decisions 64, 80): UI Toolkit or UGUI components plus USS/`Selectable` colours; write code only for rules they cannot express. Physics goes through `Packages/Motion`; never re-implement a Motion solver in a UI module. Gestures follow the single-owner rule in [指標互動與世界橋接](../Documentation/指標互動與世界橋接.md#一條規則管所有手勢). UI Toolkit code that needs its own logic lives under `Packages/UIToolkit/Runtime/WebUI/<Module>/`.
- **Accepting.** Record: `Packages/<package>/Documentation/Yu5h1Lib.UX/<Module>.md`; round: `checklists/<task-id>.checklist`. Run automated checks on a clean worktree by repointing `Yu5h1LibTest`'s `Packages/manifest.json` at it (decision `uitk-clean-checkout-host`), then restore it. Back up `manifest.json` and `packages-lock.json` first; after editing the manifest call `UnityEditor.PackageManager.Client.Resolve()` and confirm `PackageInfo.FindForAssetPath("Packages/<id>/package.json").resolvedPath` is the worktree — a force refresh alone can keep the previous resolution — and confirm it again after restoring, with the lock file's hash back to the backup, before removing the worktree. The junctions under `Yu5h1LibTest/Assets/Samples/<package>/<version>/<Name>` still point at the main checkout's `Samples~`, so a sample that references runtime types the worktree does not have fails to compile and can block the run (seen 2026-10-19 with `Yu5h1Lib.UIToolkit.Acceptance`): re-junction the sample from the worktree for the run, or keep sample assemblies out of the tested dependency chain, and record which inputs were re-pointed. PlayMode layout tests only resolve when the panel is drawn (Game view). The shared acceptance entry is each package's `Samples~/UxAcceptance` (junctioned into `Yu5h1LibTest/Assets/Samples/…`), with `CurrentTarget.json` naming the module case the user opens; visual acceptance compares side by side with the Web demo per UX decision 83. UI Toolkit and UGUI are separate implementations with separate records.

## Structure

- [skills/](skills/) — reusable techniques, separated by development scope.
- [plans/](plans/) — architecture and refactor design records.

## Skills

Read only what matches the current task:

- [Unity agent operations](skills/unity-agent-operations/SKILL.md) — read before opening, running, scanning or compile-checking any Yu5h1Lib Unity project: which editor each project uses, the one-editor-per-project rule, batchmode commands and log locations, re-runnable DryRun/Apply editor scripts, unityMCP pitfalls, compile checks on 6000.3.9f1 and 6000.6.0f1, and the route to every agent tool.

- [Unity UI extraction](skills/unity-ui-extraction/SKILL.md) — extract reusable Unity UI from consumer projects into shared packages, preserve assets/lifecycle, verify a second use, and migrate callers. Existing-control adoption routes to the relevant domain skill; Web extraction remains owned by Web.
- Adopting a Yu5h1Lib.UX module in a package has no skill of its own here: the shared spec-adoption skill plus [the section above](#adopting-a-ux-module-in-a-package).

- [data-architecture.md](skills/data-architecture.md) — ScriptableObject architecture, Parameter/Member/Invocation objects, ValuePort, Theme, and presets. Load its linked architecture plan only for design or Timeline integration changes.
- [editor-tooling.md](skills/editor-tooling.md) — Inspector and Editor extensions, SubAssets, Undo, shortcuts, and internal Unity APIs.
- [unity-event-extensions.md](skills/unity-event-extensions.md) — ParameterObject arguments, dynamic persistent arguments, MessageSender/Receiver, and UnityEvent extension techniques. It routes to the closed Invocation/Transmission design records when history is needed.
- [preferences.md](skills/preferences.md) — persisting settings with PlayerPrefs: what to use for UI-edited versus code-owned values, and wiring `Preferences` in the scene over MCP instead of writing save/load code.
- [async-task-objects.md](skills/async-task-objects.md) — `Awaitable` job objects in common (`AsyncTaskObject`, `AsyncProgressObject`, `AsyncLoadObject<T>`, `ResultJudge`): event order, cancellation, writing a judge, and awaiting a job from code.
- [loader-handler.md](skills/loader-handler.md) — loading content at runtime: `LoaderHandler` (singleton, loader field, Resources when empty), `LoaderAgent` in the scene, the consumer's `InstanceInitializer`, swapping loaders, release rules, a scene wiring example, and testing loaders in Edit Mode.
- [unity-serialization.md](skills/unity-serialization.md) — `Optional<T>`, `SerializedType`, `SerializedAssembly`, and Unity serialization boundaries.
- [gpu-rendering.md](skills/gpu-rendering.md) — procedural instancing, ComputeBuffer ownership, shader keyword variants, vertex channel payloads, struct layout shared across C#/compute/shader, and reading state out of a vendored solver.
- [unified-solver.md](skills/unified-solver.md) — assembling `Packages/Unified-Solver`: the verified reference scene and its real numbers, which components pair with which profiles, water, jets, locomotion, nets and catching, what a script can drive, the velocity-versus-position channels, and what to check when something silently does nothing. Its live state is in that package's `handoff.md` and its reasoning in `plan.md`; this skill is neither.
- [Unity CLI](skills/unity-cli/SKILL.md) — Unity's official CLI and `com.unity.pipeline`: what they add over the `unityMCP` bridge already in use, version requirements, and beta rough edges, for anyone deciding whether to adopt or troubleshoot them.

For a tracked task, open the file that declares its ID. Use [handoff.md](../handoff.md) for live coordination state and the reports for progress summaries.

Keep this file as a concise introduction. Put procedures and technical details in the matching skill.
