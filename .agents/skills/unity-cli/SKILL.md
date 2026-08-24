---
name: unity-cli
description: Install, adopt, use, or troubleshoot Unity's official CLI and com.unity.pipeline, including comparing them with the unityMCP bridge already used in this workspace.
---

# Unity CLI (official)

Use this skill when deciding whether to install, adopt, or troubleshoot Unity's
official command-line tool (`unity` / `unity.exe`) or its `com.unity.pipeline`
package, or when explaining what either one offers over the existing
`unityMCP` bridge already used in this workspace.

Not yet adopted anywhere in this workspace. Nothing below has been verified
against a Yu5h1Lib project — it is research recorded so the same ground is not
re-derived, not a confirmed workflow. Verify against the installed CLI's own
`--help` output before relying on exact flags; it is beta and changes fast.

## What it is

Two separate pieces, released together (announced Unite Seoul 2026, July 2026):

- **Unity CLI** — a standalone binary, independent of Unity Hub. Installs and
  manages Editors/modules, authenticates, and drives `build`/`test` in batch
  mode. No Editor version requirement of its own.
- **`com.unity.pipeline`** (experimental package, installed *into* a project)
  — lets the CLI drive a project's *running* Editor or a dev Player over a
  local API: play mode control, recompile, run tests, live `eval` of C#. This
  is the half that turns the CLI into an agent-operable surface, and it
  **requires Unity Editor 6.0 (6000.x) or later**. `6000.3.9f1` qualifies.

Both are distinct from `unityMCP`, the MCP server already connected in this
workspace. `unityMCP` talks to the Editor over its own socket/reflection
bridge; Unity CLI + Pipeline is Unity's own equivalent, and future MCP
bridges may end up built on top of it instead of reflection.

## Why it matters for agent work here

- **Structured output**: JSON/TSV/ndjson plus predictable exit codes — no
  Console-text scraping to decide success/failure.
- **Closed loop**: an agent can open the project, apply a change, run tests,
  enter Play Mode, query the live scene, and decide next steps without a
  human relaying output back and forth.
- **Live `eval`**: runs C# directly inside a running Editor/Player — no
  project recompile or domain reload needed for a one-off check.
- **Dynamic discovery**: project code can expose its own commands via
  `[CliCommand]`; an agent can enumerate what is callable instead of working
  from a hardcoded list.
- **Headless/CI**: no Hub install needed; fits build agents and CI runners
  that today would otherwise need a hand-rolled batchmode wrapper.

## Known beta rough edges (from community reports, not this workspace)

- Domain reload can invalidate the Pipeline session token mid-task, dropping
  the agent's connection until the Editor is reconnected.
- Modal dialogs (unsaved-scene prompts, etc.) block the CLI; `-automated`
  flag works around some of them, not all.
- Reported ~16x slower per call than a hand-rolled internal bridge in at
  least one team's benchmark — do not assume it is faster than `unityMCP`
  for this workspace without measuring.

## Learning resources

- [Official CLI docs](https://docs.unity.com/en-us/unity-cli/unity-cli) ·
  [Meet the Unity CLI (Unity blog)](https://unity.com/blog/meet-the-unity-cli)
- [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills) —
  Unity's own agent-skill repo; its `unity-cli` skill covers bootstrap,
  install, build, auth from Claude Code/Cursor/etc.
- [Vindler: what ships today, what is still broken](https://vindler.solutions/blog/unity-cli-agent-automation) —
  the most concrete hands-on account of the beta rough edges above.

## Before adopting into this workspace

- Confirm which projects would actually gain from it over `unityMCP` — the
  gain is CI/headless and structured output, not raw capability; `unityMCP`
  already gives interactive-session agents Editor control.
- Try it in a disposable test project first; it is beta and the docs warn it
  can modify or damage a project — back up / rely on version control before
  running any command against a real one.
