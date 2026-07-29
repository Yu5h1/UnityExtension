# tasks — UnityExtension routing index

> Canonical location: `C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension\.agents\tasks.md`.
> Role: canonical entry index for UnityExtension-owned tasks and Unity application validation routes.
> Use the matching `Task ID` from `report.json` when one exists. Progress belongs in `report.json` / `report.dev.json`; live coordination state belongs in the relevant `handoff.md`.
> Reusable Unity technique routing starts at [introduction.md](introduction.md).

## UnityExtension tasks

| Task ID | Canonical entry | Scope / routing note |
|---|---|---|
| `solution-inventory` | [UnityExtension handoff](../handoff.md) | Solution-level package inventory, ownership, dependency direction, and reusable capability planning |
| `recycler-refactor` | [Recycler_Refactor](plans/Recycler_Refactor.md) | Core Recycler contracts, Unity adapters, reset behavior, tests, and migration |
| `data-architecture` | [Yu5h1lib-Unity-ScriptableObject-Architecture](plans/Yu5h1lib-Unity-ScriptableObject-Architecture.md) | ParameterObject, ScriptableObject data architecture, and Timeline integration boundaries |
| `motion-system` | [Motion_System_Refactor](plans/Motion_System_Refactor.md) | Engine-independent Motion contracts, runners, and Unity migration |
| `atomic-components` | [AtomicComponents](plans/AtomicComponents.md) | Resolver, Repeater, random and shuffle components, and ScriptableObject wrappers |
| `unity-event-transmission` | [Transmission design](plans/Transmission_設計.md) | UnityEvent argument persistence, message routing, and the closed Invocation alternative |

## Unity application validation routes

| Task ID | Canonical entry | Scope / routing note |
|---|---|---|
| `bonghuo-vr` | `W:\UnityProject\BonghuoVR\handoff.md` | Current application state and validation route for reusable UnityExtension capabilities |

## Related scope indexes

- Pure Yu5h1Lib Core and package refactors: [Yu5h1Lib task routing index](../../../.agents/tasks.md).

## Maintenance rules

- Keep one row per routable task or project.
- Reuse the corresponding TaskProgress task ID when the task is reported.
- Link to one canonical entry; let that entry route to supporting documents.
- Record status, progress, next steps, and completion details in the reports or authoritative handoff, not here.
- Update this index only when a task is added, removed, renamed, transferred, or its canonical entry changes.
