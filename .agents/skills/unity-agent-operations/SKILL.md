---
name: unity-agent-operations
description: How an agent opens, runs, inspects and verifies the Yu5h1Lib Unity projects in this workspace — which project uses which editor, the one-editor-per-project rule, batchmode commands and log locations, re-runnable DryRun/Apply editor scripts, unityMCP pitfalls, compile checks on both supported editors, and the route to every agent tool. Read before starting, running, scanning or compile-checking any Yu5h1Lib Unity project.
---

# Unity agent operations

This skill is the route to the tools agents use on Unity projects here. When a tool is added
(see [Tools](#tools)), it gets a row in that table in the same change. The work plan behind these
tools is [Agent 在 Unity 的工作效率](../../../Documentation/資料驅動的Agent翻譯器.md)
(Task ID `agent-data-translator`).

## Projects and editors

Installed editors: `C:\Program Files\Unity\Hub\Editor\6000.3.9f1` and `…\6000.6.0f1`. The
`Unity` environment variable points to 6000.6.0f1's `Editor\Unity.exe`. Always take a project's
editor from its `ProjectSettings/ProjectVersion.txt`, never from the environment variable.

| Project (`W:\UnityProject\…`) | Editor |
|---|---|
| Virtual-Chat-Partner, HealthAI, KFC, Yu5h1LibTest | 6000.6.0f1 |
| BonghuoVR, MotionGen, 2D Test, AI Test, MoveToTrailUV, Test | 6000.3.9f1 |
| 2025.11.26_Abandoned.Marine.Laboratory (6000.3.8f1), My project (6000.2.7f2) | not installed |

The oldest supported editor is 6000.3.9f1 (BonghuoVR), so shared code must compile on 6000.3.9f1
and 6000.6.0f1. Projects consume UnityExtension packages through `file:` paths in
`Packages/manifest.json`, so a package edit reaches every consuming project at its next import.
Verified 2026-10-22.

## One editor per project

Unity lets one editor process open a project at a time. A project is open when
`<project>/Temp/UnityLockfile` exists (Unity deletes `Temp` on a clean exit). The user usually
keeps **Yu5h1LibTest** open in the editor. Before batchmode, check the lockfile; when the project
is open, ask the user instead — never close or kill the user's editor. Use unityMCP for a project
that is open, batchmode for one that is not.

## Batchmode

Use the runner `Tools/agent/unity_batch.py` (see [Tools](#tools)): it picks the project's editor, refuses an
open project, writes the log, enforces a timeout and summarises compile errors. The raw command it runs:

```text
"<editor>\Editor\Unity.exe" -batchmode -quit -projectPath "<project>" -logFile "<log>" [-executeMethod Namespace.Class.Method]
```

- Write logs and reports outside the repository, next to the project: the runner uses
  `W:\UnityProject\<project>-Build\logs\`; older VCP outputs are in `W:\UnityProject\VCP-Build\`.
- A run takes about 20 seconds with a warm Library and 1–2 minutes when it has to import
  (VCP 2026-10-22: 23 s). Run it once; do not loop.
- Success is exit code 0 plus no `error CS` in the log; the log ends with
  `Exiting batchmode successfully now!`. An `-executeMethod` method sets its own exit code with
  `EditorApplication.Exit`.
- Check `git status` of the project afterwards; a run may rewrite generated files, which belong
  to the project's owner, not to the agent.

## Editor scripts that change assets or scenes

Write them re-runnable, with a report-only pass first, following
`W:\UnityProject\Virtual-Chat-Partner\Assets\Editor\CatSetup.cs`:

- `DryRun` lists every change it would make and writes nothing; `Apply` writes. Both are public
  static methods so batchmode can call them with `-executeMethod`.
- Applying twice changes nothing the second time.
- Each run writes a timestamped report to the project's `-Build` folder.

## unityMCP

- `refresh_unity` may refresh without compiling; use its force mode, then confirm with
  `read_console` before using new types.
- With several Unity instances connected, call `set_active_instance` first.
- Read editor state through resources (for example `mcpforunity://editor/state`) before acting.

## Compile checks on both editors

- **Core** (`Unity/Core`): `dotnet build Yu5h1Lib.Editor.csproj -c Release -f net48 -p:Unity=<6000.3.9f1 Editor\Unity.exe> -p:OutputPath=<Unity>\bin\Validation\Core6000_3\Release\`.
  `net48` keeps the post-build copy from overwriting `Packages/Core`; the normal build is
  `netstandard2.1` against the `Unity` variable (6000.6.0f1). Core changes need the user's
  authorization.
- **Why both editors**: Core once compiled on 6000.6 but broke on 6000.3 (`AssetCreationEndAction`
  missing; `FindObjectsByType` overloads differ). `#if` cannot help a DLL compiled once outside
  Unity.
- **Package code**: `python Tools/agent/unity_compile.py <X.asmdef>` compiles the asmdef's current sources
  on 6000.3.9f1 and 6000.6.0f1 without opening Unity. It takes references and defines from a consumer
  project's generated `<Assembly>.csproj` on each editor (today BonghuoVR for 6000.3 and VCP for 6000.6),
  so it matches Unity; without one it falls back to a generic set and says so in a note. A consumer gets
  generated csproj files only after Unity has synced IDE projects there.

## Reading scene and asset data

Do not hand-parse scene, prefab or asset YAML. On 2026-10-22 a hand-written parse of VCP
ChatScene missed seven listeners and later counted neighbouring events, both silently; even the
corrected hand count double-counted the shared callbacks, which the inline SO export then exposed.
Start with the inline SO settings export for inline ScriptableObjects and their events, and the
UnityEvent report for wiring validity. When neither covers the question, say so and propose
extending one instead of trusting an ad-hoc parse.

## Tools

| Tool | Status | How to run |
|---|---|---|
| UnityEvent report (`Yu5h1Lib.EditorExtension.UnityEventReport.Run`, common Editor) | available | batchmode `-executeMethod Yu5h1Lib.EditorExtension.UnityEventReport.Run -ueScenes "Assets/A.unity;Assets/B.unity" -ueAssets "<paths>" -ueOut "<json>" [-ueBrokenOnly]`; exit code 1 means broken calls. In the editor: Tools/UnityEvent Viewer. Design: [UnityEvent監控](../../../Documentation/UnityEvent監控.md) |
| inline SO settings export (`Yu5h1Lib.EditorExtension.InlineObjectExport.Run`, common Editor) | available | `python Tools/agent/unity_batch.py <project> --method Yu5h1Lib.EditorExtension.InlineObjectExport.Run -- -ioScenes "Assets/A.unity;Assets/B.unity" -ioAssets "<paths>" -ioOut "<json>"`. Output: `totals` (listener count and dynamic count per event channel, split into `inline` objects and their `holders`), `holders` (each object holding inline SOs, with its own UnityEvents), `objects` (each inline SO: holder path, field path, type, name, every serialized field with its value, every UnityEvent channel with target, method, mode, argument, status). References are hierarchy paths, asset paths or `inline: <holder> > <field>`, never fileID or GUID; no timestamp, so two runs diff cleanly |
| GUID／asset index and reverse lookup (`Tools/agent/unity_guid.py`) | available | `python Tools/agent/unity_guid.py <project> find <guid>`, `guid <asset-path>` or `refs <guid-or-path>` (`--cache` adds Library/PackageCache, `--json` for JSON); a GUID given to `refs` must belong to an asset in the index (Library/PackageCache only with `--cache`), otherwise it is not found; exit 0 = found, 1 = not found, 2 = not a Unity project or bad arguments. Covers Assets, embedded and `file:` packages; reads only `.meta` guid lines and GUID strings |
| batchmode runner (`Tools/agent/unity_batch.py`) | available | `python Tools/agent/unity_batch.py <project> [--method Ns.Class.Method] [--timeout 600] [-- <Unity args>]`; exit 2 = refused (project open or editor missing), 124 = timed out, otherwise Unity's exit code |
| compile check without Unity (`Tools/agent/unity_compile.py`) | available | `python Tools/agent/unity_compile.py <X.asmdef> [--editors 6000.3.9f1,6000.6.0f1] [--reference-project <project>] [--define S]`; exit 1 = an editor failed |

Tools under `Tools/agent/` are standard-library Python (decision `cli-tool-home`, 2026-10-22); run them
from the UnityExtension root. The official Unity CLI is not adopted here; see [unity-cli](../unity-cli/SKILL.md).
