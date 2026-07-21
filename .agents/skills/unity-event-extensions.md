# UnityEvent extensions

Use this skill for any Yu5h1Lib task involving UnityEvent runtime behavior, persistent listeners, Inspector authoring, unsupported serialized argument types, dynamic argument overrides, or a proposal to replace UnityEvent with a custom invocation model.

This is the shared home for current and future UnityEvent extension techniques. Add a focused section here when another reusable UnityEvent technique becomes stable.

## Native-first design rule

UnityEvent already provides serialized targets, methods, listener ordering, enable state, and Inspector authoring. Identify the exact missing capability before introducing another action or invocation abstraction.

When this skill matches a task:

1. Tell the user which existing UnityEvent extension may solve the request.
2. Distinguish extending UnityEvent from replacing it.
3. Prefer the smallest existing technique that fills the capability gap.
4. Introduce a separate invocation abstraction only when UnityEvent cannot represent the required semantics.

## Technique selection

```text
Need an unsupported Inspector argument type such as Vector3 or a serializable class?
  -> Use a concrete ParameterObject<T> as UnityEvent's Object argument.

Need to change an existing persistent listener's serialized value before invocation?
  -> Use ArgumentInfo + UnityEventEx.LoadArgument.

Need path-based target resolution, arbitrary reflected signatures, reusable method assets,
multiple parameters, or explicit success/failure handling?
  -> Consider MethodObject / InvocationObject as a deliberately separate system.
```

Do not create an ActionObject merely to reproduce UnityEvent's existing target-and-method serialization.

## ParameterObject as an extended UnityEvent argument

### Purpose

UnityEvent's persistent static arguments natively cover only `int`, `float`, `string`, `bool`, and `UnityEngine.Object`. `ParameterObject<T>` uses the Object argument slot to carry a typed serialized value that UnityEvent cannot represent directly, such as `Vector3` or a Unity-serializable class.

The conceptual flow is:

```text
UnityEvent persistent listener
  target + method are still native UnityEvent data
        |
        v
Object argument -> concrete ParameterObject<T>
        |
        v
listener receives the ParameterObject and reads its typed value
```

### Editor integration

- `UnityEventPropertyMenu` reads the selected method's object-argument type and offers creation of compatible concrete ScriptableObject types.
- `UnityEventCompactDrawer` recognizes a ScriptableObject in `m_ObjectArgument` and draws its serialized fields inline inside the listener row.
- A simple `ParameterObject<T>` therefore exposes its `value` directly in the UnityEvent Inspector instead of forcing the user to open a separate asset Inspector.
- Inline instances referenced from a Scene can be serialized with the Scene. Asset-owned instances must use correct sub-asset ownership and Undo handling.

Relevant files:

- `Packages/common/Runtime/Data/Architecture/ParameterObject.cs`
- `Packages/common/Editor/Utility/ParameterObjectUtility.cs`
- `Internal/com.yu5h1.Internal/Editor/PropertyDrawer/UnityEventCompactDrawer.cs`
- `Internal/com.yu5h1.Internal/Editor/MenuItem/UnityEventPropertyMenu.cs`
- `Editor/Base/Source/Extension/SerializedPropertyEx.cs`

### Responsibility boundary

- In this technique, `ParameterObject` is the serialized Object argument and typed value source.
- `ParameterObject.GetValue()` and its typed `value` are relevant to consuming the argument.
- `ParameterObject.ApplyTo(target)` is a later, separate property-application capability. Do not describe `ApplyTo` as part of UnityEvent invocation.
- The ParameterObject technique does not mutate UnityEvent's persistent `ArgumentCache` before every call; it supplies a stable Object reference whose internal value is editable and serializable.

## Dynamic persistent arguments

### Design decision

The capability gap here is UnityEvent's lack of a public API for overriding a persistent listener's serialized argument at runtime. Prefer augmenting the existing UnityEvent through `ArgumentInfo` before creating a parallel event framework.

This technique changes the cached static argument immediately before calling the original UnityEvent. It is not the same as passing the runtime parameter of `UnityEvent<T>`.

### Existing flow

```text
MessageSender
  message -> List<ArgumentInfo>
        |
        v
MessageReceiver.TryInvoke(message, arguments)
        |
        v
UnityEventEx.LoadArgument(argument)
  finds a matching persistent call
  updates its serialized ArgumentCache through reflection
  dirties UnityEvent's persistent-call cache
        |
        v
UnityEvent.Invoke()
```

Relevant files in `UnityExtension`:

- `Packages/common/Runtime/Transmission/MessageSender.cs`
- `Packages/common/Runtime/Transmission/MessageReceiver.cs`
- `Packages/common/Runtime/Event/ArgumentInfo.cs`
- `Packages/common/Runtime/Extension/UnityEventEx.cs`
- `Packages/common/Editor/PropertyDrawer/ArgumentInfoDrawer.cs`
- `Packages/common/Editor/MenuItem/ArgumentInfoPropertyMenu.cs`

### ArgumentInfo responsibility

`ArgumentInfo` is the serialized description used to locate and update one persistent listener argument. It contains:

- listener identity: target name, method name, and `PersistentListenerMode`;
- the value fields Unity serializes for `int`, `float`, `string`, `bool`, and `UnityEngine.Object` arguments;
- object argument type metadata where needed by Editor serialization.

`ArgumentInfo` describes an argument override; it does not replace the UnityEvent listener itself.

### ArgumentPayload history

The abandoned `UnityExtension-codex` prototype used an abstract `ArgumentPayload<T>` hierarchy for the same argument-description responsibility. The current design replaced that hierarchy with the single union-like `ArgumentInfo` type.

`Packages/common/Runtime/Component/ArgumentPayload.cs` in the current tree is only a `MonoBehaviour` wrapper around one `ArgumentInfo`. It is not part of the MessageSender-to-MessageReceiver execution flow and has no behavior of its own. Treat it as optional legacy/experimental scaffolding unless a concrete scene-component use case is established.

### Reflection boundary and limitations

- `UnityEventEx` reflects Unity's internal persistent-call fields such as `m_PersistentCalls`, `m_Calls`, `m_Arguments`, and the typed argument fields.
- Keep this reflection isolated and fail safely when Unity changes an internal member.
- Current support is limited to Unity's persistent static argument modes: `int`, `float`, `string`, `bool`, and `UnityEngine.Object`.
- Matching by target name can be ambiguous when multiple targets share a name. Method name and listener mode narrow the match but do not uniquely identify duplicate persistent calls.
- After changing an argument, dirty UnityEvent's persistent calls before invocation so its runtime call list is rebuilt.

## When a separate invocation model is justified

`MethodObject` / `InvocationObject` are not general replacements for UnityEvent. Consider them only when a request needs capabilities such as:

- resolving a target from a child path and component type;
- invoking arbitrary public methods with multiple serialized `ParameterObject` values;
- reusable method or invocation assets independent of a UnityEvent field;
- exact reflected overload selection;
- explicit `TryInvoke` success/failure semantics and fail-fast sequencing.

When those capabilities are not required, retain native UnityEvent and apply one of the extensions above.
