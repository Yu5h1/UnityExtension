# Loader handler, agent and initializer

How to load content at runtime through `com.yu5h1.addressables` (`Packages/Addressables/Runtime`, namespace
`Yu5h1Lib.AddressableAssets`) and the loaders in common (`Packages/common/Runtime/Loader`, namespace `Yu5h1Lib`).
Design and reasons: [Addressables plan § 首輪範圍](../../Documentation/Addressables/plan.md#首輪範圍讀取處理器代理與角色載入)
(Task ID `addressables-package`). Events and cancellation of the request come from
[async-task-objects.md](async-task-objects.md). Tests that show every rule below:
`Packages/common/Tests/Editor/ResourcesLoaderTests.cs`, `Packages/Addressables/Tests/Editor/LoaderHandlerTests.cs`,
`LoaderAgentTests.cs`, `AddressablesLoaderTests.cs`.

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
| `AddressablesLoader` | Addressables | Addressables by key (address, or a label: first match). Looks the key's location up first, so a missing key returns null with no error log; progress is the operation's `PercentComplete`, 1 at the end; cancellation throws at once and the handle is released when Addressables finishes; a failed load releases and rethrows. Holds one handle per returned object until `Release(asset)` (once per load); destroying the loader releases nothing. Local or remote is the group's load path, not the loader |
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

For Addressables, put `AddressablesLoader` next to the handler and drag it into the field. The content must be built
(Addressables Groups > Build, or `Yu5h1Lib.EditorExtension.AddressablesContentBuild.Build()`) and the Play Mode Script set to
**Use Existing Build**; `Use Asset Database` is never the acceptance path ([plan § 目的與固定方向](../../Documentation/Addressables/plan.md#目的與固定方向)).
A local group uses the default `Local.BuildPath`／`Local.LoadPath`: the player reads it from StreamingAssets `aa`, the
editor's Use Existing Build from `Library/com.unity.addressables/aa/<platform>`.

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

The Yu5h1LibTest scene `Assets/AddressablesTest/AddressablesTest.unity`. The test character is the prefab
`Assets/AddressablesTest/Content/TestCharacter.prefab` (with `TestCharacter.overrideController`) in the local group
`AddressablesTestCharacters`, address `characters/test-character`; the Resources copy in
`Assets/AddressablesTest/Resources` stays for the Resources loader.

```text
CharacterAgent (GameObject)
  LoaderAgent             initializer → TestCharacterInitializer; request judge → AcceptLoadedJudge (inline);
                          request events: Begin → LoadEventLog.Begin, End → LoadEventLog.End,
                          Loaded → LoadEventLog.Loaded (dynamic)
  TestCharacterInitializer
  LoadEventLog
  MonoEventHandler        Start → LoaderAgent.Prepare("characters/test-character")
LoaderHandler             LoaderHandler (loader → AddressablesLoader), AddressablesLoader
LoadStatusMonitor         test-only IMGUI label: idle / loading n% / succeeded / failed, from the request's events
WrongKeyTrigger           inactive; MonoEventHandler Enabled → LoaderAgent.Prepare("characters/missing-character")
```

A request without a judge raises no True／False／Completed (`AsyncLoadObject<T>` fails a missing object without asking
the judge, but only a judge raises those events), so anything that must show a failure needs a judge on the request;
the test scene uses `AcceptLoadedJudge`, which accepts every loaded object.

`MonoEventHandler` (common) raises Start, Enabled and Disabled; call the agent from Start, which runs after every
Awake and OnEnable of the scene, so the handler and the listeners already exist. Wire the request's events
persistently in the Inspector, or subscribe from code in Awake.

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
Build Addressables content with `AddressablesContentBuild` (unity-agent-operations, Tools), not
`BuildPlayerContent`: the first build in a project otherwise opens a modal 'Debug Build Layout' dialog that blocks the
editor and every unityMCP call until a person answers. Loading built Addressables content is not an EditMode test:
verify it in Play Mode, as the remote tests below do.

Test-only Resources assets go under the test assembly's `Editor/Resources/<unique folder>`: Resources.Load finds them
in the editor and they never reach a build.

## Remote content on this machine

The loader is the same for local and remote content; a group's load path decides. Yu5h1LibTest's remote test is set up
by `Assets/AddressablesTest/Editor/RemoteTestSetup.cs` (re-runnable; `--method RemoteTestSetup.Run` sets up and builds):

```text
group AddressablesTestRemote   Remote.BuildPath ServerData/[BuildTarget]
                               Remote.LoadPath  http://127.0.0.1:8002/[BuildTarget]  (Default profile)
entry characters/test-character-remote → Assets/AddressablesTest/Content/TestCharacterRemote.prefab
Player Settings > Allow downloads over HTTP: Always allowed (Unity blocks http:// otherwise)
catalog: stays in the build (local); a remote catalog belongs to content updates
server:  python localHost.py --root W:\UnityProject\Yu5h1LibTest\ServerData --host 127.0.0.1 --port 8002 --no-browser
         (LocalWebService; ask the user before starting it, stop it afterwards)
```

PlayMode tests `Assets/AddressablesTest/Tests/RemoteLoadTests.cs` (assembly `Yu5h1LibTest.AddressablesTest.Tests`) run
in batchmode with `-runTests -testPlatform PlayMode -assemblyNames Yu5h1LibTest.AddressablesTest.Tests -testCategory <c>`:
`RemoteServerUp` with the server running (bytes to download before the load, 0 after: cached), then `RemoteServerDown`
after stopping it (the cached bundle loads; after clearing the cache the load fails with a Dependency Exception).
Two things the tests had to learn: a released bundle unloads asynchronously, so `ClearDependencyCacheAsync` right after
`Release` fails until it has unloaded (retry over frames); and `Assert.CatchAsync`/`ThrowsAsync` around a pending
`Awaitable` deadlocks the main thread (async-task-objects, Tests). LocalWebService defaults to
`Cache-Control: no-store`; the Editor and a PC player cache bundles by hash, while a WebGL player uses browser caching.
For the browser cache experiment explicitly use `--cache-max-age 3600`; default development behavior stays no-store.
A warm load may make a conditional request (304, zero body bytes), so distinguish request count from body transfer.
The cache option and its error/control exclusions are owned by LocalWebService's `skills/local-web-service/SKILL.md`.

### WebGL probe

`python Tools/agent/unity_addressables_webgl.py W:\UnityProject\Yu5h1LibTest` builds an isolated player to
`W:\UnityProject\Yu5h1LibTest-Build\addressables-webgl`, with timestamped backups in its sibling `backup` folder.
It requires approval for the long build and temporary Player Settings changes. It temporarily calls the existing
`RemoteTestSetup.Apply`, builds content through `AddressablesContentBuild`, builds only the probe scene, and restores
Player/User/Addressables asset settings in `finally`. It refuses differing preexisting probe sources.
The fixture lives under `Tools/agent/fixtures/AddressablesWebGL`; its page reports a non-null instantiated remote
character, then releases it. It is a loading test, not the user's visual/Inspector introduction check.

Serve the player directory and mount `/WebGL` to Yu5h1LibTest's `ServerData/WebGL`, using only approved loopback
test services. At port 8002, `probe.html?remote-test=1&same-origin=1` uses the page origin; at port 8000,
`probe.html?remote-test=1` uses content on port 8002. Explicitly allow `http://127.0.0.1:8000` on the content service.
Changing the `run` query creates a fresh player for a warm run; inspect real bundle response bodies and browser logs.
Stop both test services afterwards. Per-round evidence lives in `checklists/remote-test-webgl.checklist`.

### GitHub Pages

`Assets/AddressablesTest/Editor/WebGLTestBuild.cs` in Yu5h1LibTest builds the content and a WebGL player of the test
scene `AddressablesTest.unity` to `W:/UnityProject/Yu5h1LibTest-Build/WebGL/AddressablesTest`, for upload to a static
host. Compression is off for that build only (GitHub Pages sends no Content-Encoding for Brotli/Gzip files); the
project's setting is restored and the target switched back to Windows. The editor must be closed; about 5 minutes:

```text
python Tools/agent/unity_batch.py W:/UnityProject/Yu5h1LibTest --method WebGLTestBuild.Run -- -buildTarget WebGL
```

Only local groups work there: their bundles sit in `StreamingAssets/aa` beside `Build/` and the page fetches them from
its own site over https; a load path of `http://127.0.0.1:…` would not load from a published page. The user uploaded
the folder to GitHub Pages on 2026-10-27 and the test character loaded from its bundle. The page has no Hierarchy, so
`WrongKeyTrigger` is not usable there, and IMGUI's default WebGL font has no CJK glyphs, so the test labels are English.
