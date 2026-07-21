# tasks — Unity scope overview

> Single source of status for Unity-related plans and game projects under `W:\UnityProject`.
> Role: **routing + progress overview only** — no detail (detail lives in each `plans/*.md`, or a project's `plan.md` / `agent.md`).
> Status legend: ✅ done / 🔧 has a clear actionable next step / 📐 in design (needs a decision first) / 🧩 mostly done, tail remains / 📦 to archive.
> Last updated: 2026-07-03 (path re-org; done/not-done status pending reconciliation).
>
> Note: pure Yu5h1Lib Core refactor track moved to `C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\.agents\tasks.md`.

---

## Unity-related refactor / architecture track

| Status | Plan | Next step (smallest actionable unit) |
|--------|------|--------------------------------------|
| 🔧 | [Recycler_Refactor](plans/Recycler_Refactor.md) | Core ✅ (2026-05-21). Left: Core unit tests + Unity Phase 1.5 (`UnityObjectPoolAdapter`/`Recyclable`/`RecyclerEx`/`Recycler.Reset()`) + issue 13 (`IRecyclable` hardening — discuss first) |
| 🧩 | [Yu5h1lib-Unity-ScriptableObject-Architecture](plans/Yu5h1lib-Unity-ScriptableObject-Architecture.md) | ParameterObject core ✅. Left: Timeline integration (`ParameterSignal`/`DirectorController`) + unresolved naming conflict |
| 📐 | [Motion_System_Refactor](plans/Motion_System_Refactor.md) | interpolation as a Core abstraction + lazy-init injected compute source + TimerRunner fallback. Blocked on **Q-A~Q-J answers**; not started |
| 🔧 | [AtomicComponents](plans/AtomicComponents.md) | `IResolver`/`Resolver<T>`/`Repeater` ✅ (2026-06-12, try-pattern + event base, replaces Counter). Next: decide Random backend → `RandomResolver` |

---

## Game-project track

### Bonghuo VR (BonghuoVR) — 🔧 ~33%, Act 1 "night-sea fire fishing" (8-min interactive)

| Status | Item | Entry |
|--------|------|-------|
| 🔧 | Full project description (original 31-day plan, partly superseded) | `BonghuoVR\plan.md` |
| 🔧 | Working reference / status cache | `BonghuoVR\agent.md` |
| 📊 | Human progress report | `docs\pages\bonghuo-vr.html` (for people; user asks Claude to update) |

> Critical path: InteractionController → hover detection ×3 → Timeline marker wiring (IntObject) → fishing action sequence.

---

## Maintenance rules
- Touch any plan → come back and update its row's status and "next step".
- New plan → drop it in `plans/` and add a row to the right track.
- New game project → add a section pointing to its `plan.md` / `agent.md`.
- Progress detail goes in each project's `agent.md`; this file is routing and overview only.
