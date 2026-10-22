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

```text
"<editor>\Editor\Unity.exe" -batchmode -quit -projectPath "<project>" -logFile "<log>" [-executeMethod Namespace.Class.Method]
```

- Write logs and reports outside the repository, next to the project:
  `W:\UnityProject\<project>-Build\` (VCP uses `W:\UnityProject\VCP-Build\`).
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
- **Package code**: there is no reusable command yet; the planned tool is tier 1 step 4. Until
  then, compile the changed files in a scratch project that references both editors'
  `Data\Managed\UnityEngine\*.dll` and the Core DLLs, or run batchmode on a 6000.3.9f1 and a
  6000.6.0f1 project that consume the package.

## Reading scene and asset data

Do not hand-parse scene, prefab or asset YAML. On 2026-10-22 a hand-written parse of VCP
ChatScene missed seven listeners and later counted neighbouring events, both silently. Use a tool
from [Tools](#tools) that reads through Unity; when none covers the question, say so and propose
extending one instead of trusting an ad-hoc parse.

## Tools

| Tool | Status | How to run |
|---|---|---|
| UnityEvent report (`Yu5h1Lib.EditorExtension.UnityEventReport.Run`, common Editor) | available | batchmode `-executeMethod Yu5h1Lib.EditorExtension.UnityEventReport.Run -ueScenes "Assets/A.unity;Assets/B.unity" -ueAssets "<paths>" -ueOut "<json>" [-ueBrokenOnly]`; exit code 1 means broken calls. In the editor: Tools/UnityEvent Viewer. Design: [UnityEvent監控](../../../Documentation/UnityEvent監控.md) |
| inline SO settings export (translator) | planned, item `translator-inline-so-export` | — |
| GUID／asset index and reverse lookup | planned, waits on decision `cli-tool-home` | — |
| batchmode runner | planned, waits on decision `cli-tool-home` | — |
| compile check without Unity | planned, waits on decision `cli-tool-home` | — |

The official Unity CLI is not adopted here; see [unity-cli](../unity-cli/SKILL.md).
