# Unity data architecture

Use this skill for Yu5h1Lib Unity data modeling: ScriptableObject architecture, Parameter/Member/Invocation objects, ValuePort, Theme, presets, and their runtime relationships.

## Scope

- Treat `Packages/common/Runtime/Data/Architecture/` as the home of reusable ScriptableObject data objects.
- Treat `Packages/common/Runtime/Data/ValuePort.cs` and `Packages/common/Runtime/Adapter/ValuePortAdapter.cs` as the Unity-facing ValuePort layer.
- Keep runtime code in `Yu5h1Lib`; use `Yu5h1Lib.EditorExtension` only for Editor code.
- Read [Yu5h1lib-Unity-ScriptableObject-Architecture.md](../plans/Yu5h1lib-Unity-ScriptableObject-Architecture.md) when changing the architecture or Timeline integration. Do not load that plan for routine use of existing types.

## Architecture map

- `MemberObject` is the ScriptableObject base for reusable member descriptions.
- `ParameterObject : MemberObject, IParameter` represents a serializable parameter.
- `ParameterObject<T>` stores a typed value.
- `InvocationObject` composes method invocation data and its parameter sub-assets.
- `GenericComponentPresetObject` is a ParameterObject for component preset data.
- `Theme` groups reusable parameter data for application to targets.
- `ValuePort` bridges Unity components to the Core MVVM `IValuePort` contract.
- `ValuePortAdapter<T, TValue>` adapts existing Unity Components without requiring wrapper components.

## Working rules

- Prefer an existing data abstraction before introducing another ScriptableObject family.
- Keep ParameterObject, ValuePort, and adapters under the data concern even when their consumers are UI or Timeline.
- Preserve the Core MVVM contract: bind through `IValuePort`; do not reintroduce deleted binding abstractions.
- When adding a concrete `ParameterObject<T>`, place it with the existing architecture objects and ensure Editor discovery can find a non-abstract implementation.
- Build the complete port map before writing bindings, then bind to the current data source.
- Keep Theme and preset runtime models here; route their custom Inspector and sub-asset manipulation to [editor-tooling.md](editor-tooling.md).

## Sub-asset model

- ScriptableObject children are sub-assets of the main asset; Unity does not provide nested sub-assets.
- Keep ownership explicit so removing a member or parameter also removes its owned sub-assets with Undo support.
- Do not infer ownership from names when a direct object relationship is available.
