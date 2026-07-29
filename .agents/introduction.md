# Unity agent knowledge

This directory is the canonical AI-agent knowledge entry for UnityExtension and reusable Yu5h1Lib Unity development:

`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension\.agents`

## Entry points

- Current UnityExtension state and next steps: [handoff.md](../handoff.md).
- Tracked task IDs, ownership, and canonical entries: [tasks.md](tasks.md).
- Viewer-facing progress: [report.json](../report.json) with optional [report.dev.json](../report.dev.json) developer details.
- Architecture and refactor design records: [plans/](plans/).
- Reusable Unity techniques: [skills/](skills/), loading only the file that matches the task.

## Structure

- [skills/](skills/) — reusable techniques, separated by development scope.
- [plans/](plans/) — architecture and refactor design records.
- [tasks.md](tasks.md) — stable routing from tracked task IDs to canonical entries.

## Skills

Read only what matches the current task:

- [data-architecture.md](skills/data-architecture.md) — ScriptableObject architecture, Parameter/Member/Invocation objects, ValuePort, Theme, and presets. Load its linked architecture plan only for design or Timeline integration changes.
- [editor-tooling.md](skills/editor-tooling.md) — Inspector and Editor extensions, SubAssets, Undo, shortcuts, and internal Unity APIs.
- [unity-event-extensions.md](skills/unity-event-extensions.md) — ParameterObject arguments, dynamic persistent arguments, MessageSender/Receiver, and UnityEvent extension techniques. It routes to the closed Invocation/Transmission design records when history is needed.
- [unity-serialization.md](skills/unity-serialization.md) — `Optional<T>`, `SerializedType`, `SerializedAssembly`, and Unity serialization boundaries.

For a tracked task, start from [tasks.md](tasks.md) and open its canonical entry. Use [handoff.md](../handoff.md) for live coordination state and the reports for progress summaries.

Keep this file as a concise introduction. Put procedures and technical details in the matching skill.
