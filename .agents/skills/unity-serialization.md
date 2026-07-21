# Unity serialization

Use this skill when designing or changing Yu5h1Lib values that cross Unity serialization, Inspector, assembly, or type-selection boundaries.

## Scope

- `Packages/common/Runtime/Data/Optional.cs`
- `Packages/common/Runtime/Data/SerializedType.cs`
- `Packages/common/Runtime/Data/SerializedAssembly.cs`
- Corresponding drawers under `Packages/common/Editor/`

## Working rules

- Design for Unity's serialized representation first; do not assume arbitrary .NET types survive serialization.
- Keep runtime wrappers independent of `UnityEditor`.
- Store stable type or assembly identifiers and resolve them defensively.
- Treat renamed types, missing assemblies, and unresolved serialized values as expected migration states rather than exceptional editor crashes.
- Keep the stored value distinct from its Inspector display label.
- Route generic ScriptableObject ownership and composition to `data-architecture.md`; route drawers and option menus to `editor-tooling.md`.

## `Optional<T>`

- Use `Optional<T>` when a value needs an explicit enabled/disabled state in serialized data.
- Read through `TryGetValue` rather than treating a default `T` as evidence that the option is disabled.
- Preserve the Toggle + Value Inspector representation when extending its drawer behavior.

## Serialized type and assembly selectors

- Use `SerializedType` and `SerializedAssembly` instead of serializing `System.Type` or `Assembly` directly.
- Reuse the registered option-provider pipeline for Inspector selection.
- Keep assembly-qualified names or other stored identifiers unchanged when only improving how options are displayed.
