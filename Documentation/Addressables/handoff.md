# Addressables package handoff

Task ID `addressables-package` (declared in [plan.md](plan.md); card in the root `report.json`).

## Scope and state

- Canonical design: [plan.md](plan.md). Authored 2026-09-09 as a learning plan under `Documentation/RemoteContent/`; renamed to `Documentation/Addressables/` on 2026-10-21 when the package was decided.
- Package decided and skeleton created 2026-10-21: `Packages/Addressables`, `com.yu5h1.addressables` 0.1.0, namespace `Yu5h1Lib.AddressableAssets`, depends on common and `com.unity.addressables` 2.8.1+; Runtime asmdef `Yu5h1Lib.Addressables` (references Yu5h1Lib.Common, Unity.Addressables, Unity.ResourceManager); `.meta` GUIDs fixed. No Editor asmdef yet (added when the publishing workflow needs it). First-round step 1 code since `586ea84` (below); Yu5h1LibTest imports the package (`file:` dependency plus `testables`, 2026-10-23), resolving `com.unity.addressables` 2.11.2.
- First round decided 2026-10-21 ([首輪範圍](plan.md#首輪範圍讀取處理器代理與角色載入)): a singleton loader handler with swappable loaders plus a non-singleton scene agent (`Prepare(path)`-style notify, holds `AsyncProgressObject` requests, decides release), validated first with the Resources loader, then the Addressables loader on a local group; characters delivered as prefab + Animator Override Controller with the consumer's initialization adding the rest; batching needs no special technique (any event calls the agent); only then remote content tests; Git LFS separation and the Windows A/B/C avatar prototype after those.
- Loader base and the Resources loader belong in common, the Addressables loader here; no StreamingAssets loader (Addressables local groups cover it). Events and task objects are owned by [非同步工作](../非同步工作.md) (Task ID `async-task`).
- First consumer: VCP (`W:\UnityProject\Virtual-Chat-Partner`, Task ID `streaming-assets`), WebGL, character models. VCP is still waiting for art before its Cat delivery. VCP-specific stages stay in VCP's `Documentation/流式資源下載.md` (updated 2026-10-21 to this first round).
- Still proposals, not contracts: component/service/presentation responsibilities, the A/B/C avatar flow.

## Next steps

- `loader-handler` implemented 2026-10-23 (`586ea84`) and released for independent acceptance (`checklists/loader-handler.checklist` item 6): `AssetLoader`/`ResourcesLoader` in common `Runtime/Loader`; `LoaderHandler`, `LoaderAgent`, `InstanceLoad`, `InstanceInitializer` in `Packages/Addressables/Runtime`; usage in [loader-handler](../../.agents/skills/loader-handler.md). Yu5h1LibTest (no git) test folder `Assets/AddressablesTest`, scene `Assets/AddressablesTest/AddressablesTest.unity`; EditMode tests `Yu5h1Lib.Addressables.Editor.Tests` plus common's `ResourcesLoaderTests`. Open for the user: the plan's MonoBehaviourEvent does not exist, the scene uses `MonoEventHandler` Enabled instead ([首輪範圍](plan.md#首輪範圍讀取處理器代理與角色載入)). Next: the Addressables loader in `Packages/Addressables/Runtime`.
- Gotcha: in Edit Mode an unfocused editor barely runs the player loop, so code awaiting `Awaitable.NextFrameAsync` stalls in EditMode tests run over unityMCP; await the operation (`Awaitable.FromAsyncOperation`) instead. Details in the loader-handler skill.
- [首輪（VCP）](plan.md#首輪vcp) settled 2026-10-23 as recommended (character-specific data in the main program keyed by key, applied by the consumer's initializer; validate in Yu5h1LibTest before VCP's delivery); the user also set the loader as a MonoBehaviour field on the handler and the initializer as a base class the consumer subclasses ([首輪範圍](plan.md#首輪範圍讀取處理器代理與角色載入)).
- Use [驗收清單](plan.md#驗收清單) to capture network/build-layout evidence as each prototype stage runs.

## Open decisions

- Loading presentation (blocking vs non-blocking, failure handling): UX decision 98 in `UX/decisions/ux-foundation.decisions`.
- [頭像原型（遠端測試之後）](plan.md#頭像原型遠端測試之後): avatar overlay meaning, nonblocking menu entry.

## Ruled-out directions

- Separate local Asset Database runtime and remote production implementations: user explicitly wants one remote path.
- Git LFS URLs as runtime Addressables hosting: source assets require Unity content builds; R2 serves build outputs.
- Automatically preloading both permitted avatars before menu entry was replaced by initial A first and event-triggered B, to expose visible asynchronous work in the first validation case.
- Large models/scenes are deferred extensions; requiring them for the first proof would obscure the user's avatar-first learning objective.
- Putting the capability in `com.yu5h1.common` behind `versionDefines` (2026-10-21): the user treats `versionDefines` as a last resort (Animation uses it for Splines only because it had to); a package whose subject is Addressables content depends on Addressables directly.
- A Network package holding content loading and `AsyncTaskObject` (2026-10-21): Addressables content is not only network content, and `AsyncTaskObject` is not only used for network requests. Network stays a future category for communication only.
- "No loader interface in the first round; Addressables is the only implementation" (design 2026-10-21, removed the same day): the user overturned it; the handler must be able to swap loaders, and a Resources loader is needed to validate the handler and agent before Addressables and to load built-in fallback content. A StreamingAssets loader stays out: Addressables local groups already load AssetBundles from StreamingAssets.
- Package names RemoteContent, CDN, Content (2026-10-21): "Remote" misses local groups; CDN names the hosting side, which the package never touches; UnityExtension names packages after the Unity feature they extend.
