# Unity agent knowledge

This directory contains the AI-agent knowledge for Yu5h1Lib Unity development.

## Structure

- `skills/` — reusable techniques, separated by development scope.
- `plans/` — architecture and refactor design records.
- `tasks.md` — overview and routing for active Unity work.

## Skills

Read only what matches the current task:

- `skills/data-architecture.md` — ScriptableObject architecture, Parameter/Member/Invocation objects, ValuePort, Theme, and presets.
- `skills/editor-tooling.md` — Inspector and Editor extensions, SubAssets, Undo, shortcuts, and internal Unity APIs.
- `skills/unity-event-extensions.md` — ParameterObject arguments, dynamic persistent arguments, MessageSender/Receiver, and future UnityEvent extension techniques.
- `skills/unity-serialization.md` — `Optional<T>`, `SerializedType`, `SerializedAssembly`, and Unity serialization boundaries.

Keep this file as a concise introduction. Put procedures and technical details in the matching skill.
