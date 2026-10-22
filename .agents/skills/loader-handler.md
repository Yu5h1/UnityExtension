# Loader handler, agent and initializer

How to load content at runtime through `com.yu5h1.addressables` (`Packages/Addressables/Runtime`, namespace
`Yu5h1Lib.AddressableAssets`) and the loaders in common (`Packages/common/Runtime/Loader`, namespace `Yu5h1Lib`).
Design and reasons: [Addressables plan § 首輪範圍](../../Documentation/Addressables/plan.md#首輪範圍讀取處理器代理與角色載入)
(Task ID `addressables-package`). Events and cancellation of the request come from
[async-task-objects.md](async-task-objects.md). Tests that show every rule below:
`Packages/common/Tests/Editor/ResourcesLoaderTests.cs`, `Packages/Addressables/Tests/Editor/LoaderHandlerTests.cs`,
`LoaderAgentTests.cs`.

## Components

```text
scene event (Start/Enabled, button, scroll…)
  → LoaderAgent.Prepare(key)                 one per kind of content, in the scene, not a singleton
      request: InstanceLoad (inline SO)      Begin(key) → Progress → End(key) → judge → Loaded(instance) → True/False → Completed
      → LoaderHandler.instance.loader        singleton; loader field, empty = ResourcesLoader on the handler
          → AssetLoader.LoadAsync<GameObject>(key, progress, token)
      → Instantiate(prefab, parent)
      → initializer.Initialize(instance, key)   optional InstanceInitializer on the agent, before Loaded
```

| Type | Where | Is |
|---|---|---|
| `AssetLoader` | common | Abstract MonoBehaviour: `LoadAsync<T>(key, IProgress<float>, CancellationToken)` → `Awaitable<T>`, null when nothing is at the key (never throws for that); cancellation throws `OperationCanceledException`. `Release(asset)` gives back what it loaded (no-op by default) |
| `ResourcesLoader` | common | `Resources.LoadAsync`; key = path under a Resources folder. Awaits the request itself (no frame polling); progress is reported at start and as 1 at the end |
| `LoaderHandler` | Addressables | `SingletonBehaviour<LoaderHandler>`: scene instance, else a `LoaderHandler` prefab in Resources, else a new one. `loader` = the field, or a `ResourcesLoader` added to the handler when the field is empty. `LoadAsync<T>` for code |
| `LoaderAgent` | Addressables | Scene stand-in: `Prepare(string)` for UnityEvents, `PrepareAsync(key, token)` → `Awaitable<bool>` for code; fields `_request` (`InstanceLoad`, `[Inline]`), `_initializer`, `_parent`; `instance`, `key`, `Release()` |
| `InstanceLoad` | Addressables | The agent's `AsyncLoadObject<GameObject>`; wire the loading presentation to its events. The agent sets its key, so it has no key setting |
| `InstanceInitializer` | Addressables | Abstract MonoBehaviour the consumer derives from: `Initialize(GameObject instance, string key)` |

## Swapping the loader

Put a component deriving from `AssetLoader` on the handler (or anywhere) and drag it into the handler's loader field;
empty means Resources. To set it up once for every scene, make a prefab named `LoaderHandler` in a Resources folder
with the handler and its loader; `LoaderHandler.instance` instantiates it when the scene has none. From code:
`LoaderHandler.instance.loader = myLoader;` (null goes back to Resources).

A loader that holds resources (Addressables handles) overrides `Release`; the agent calls it on the loader that
loaded the asset, not on whatever the handler holds now.

## Consumer initializer

```csharp
public class CharacterInitializer : InstanceInitializer
{
    public override void Initialize(GameObject instance, string key)
    {
        instance.AddComponent<AudioSource>();
        // look up this key's character data and apply it
    }
}
```

Put it on the agent's GameObject and drag it into the agent's initializer field. It runs once per new instance,
before Loaded and before the judge's True/False, so listeners of Loaded see an initialized instance. The package
never references consumer components; whatever the prefab lacks is added here.

## Scene wiring example

The Yu5h1LibTest scene `Assets/AddressablesTest/AddressablesTest.unity` (test character in
`Assets/AddressablesTest/Resources`):

```text
CharacterAgent (GameObject)
  LoaderAgent             initializer → TestCharacterInitializer; request events: Begin → LoadEventLog.Begin,
                          End → LoadEventLog.End, Loaded → LoadEventLog.Loaded (dynamic)
  TestCharacterInitializer
  LoadEventLog
  MonoEventHandler        Enabled → LoaderAgent.Prepare("AddressablesTestCharacter")
```

Common has no Start event component; `MonoEventHandler`'s Enabled (OnEnable) is the scene-start trigger. Wire the
request's events persistently in the Inspector: a component subscribing from code in Awake or OnEnable may run after
the Enabled call that already started the load.

## Release rules

- The agent owns one instance and the asset it came from.
- Loading another key replaces the instance only after the new one is ready; the old one stays visible meanwhile.
- `Prepare` of the key already shown does nothing; of the key being loaded, waits for that load; of another key,
  cancels the running load first.
- Failed, rejected (judge) or canceled loads destroy their instance and release their asset; the current instance
  stays.
- `Release()` and destroying the agent destroy the instance and call the loader's `Release`.

## Testing loaders in Edit Mode

An unfocused editor barely runs the player loop in Edit Mode (`Time.frameCount` moved 5 frames in 71 s on
2026-10-23), and `EditorApplication.QueuePlayerLoopUpdate` does not help, so anything awaiting
`Awaitable.NextFrameAsync` stalls in an EditMode test run from unityMCP. Await the operation instead
(`Awaitable.FromAsyncOperation`), as `ResourcesLoader` does, or hold the work on an `AwaitableCompletionSource<T>`.
Test-only Resources assets go under the test assembly's `Editor/Resources/<unique folder>`: Resources.Load finds them
in the editor and they never reach a build.
