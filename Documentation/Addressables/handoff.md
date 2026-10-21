# Addressables package handoff

Task ID `addressables-package` (declared in [plan.md](plan.md); card in the root `report.json`).

## Scope and state

- Canonical design: [plan.md](plan.md). Authored 2026-09-09 as a learning plan under `Documentation/RemoteContent/`; renamed to `Documentation/Addressables/` on 2026-10-21 when the package was decided.
- Package decided and skeleton created 2026-10-21: `Packages/Addressables`, `com.yu5h1.addressables` 0.1.0, namespace `Yu5h1Lib.AddressableAssets`, depends on common and `com.unity.addressables` 2.8.1+; Runtime asmdef `Yu5h1Lib.Addressables` (references Yu5h1Lib.Common, Unity.Addressables, Unity.ResourceManager); `.meta` GUIDs fixed. No Editor asmdef yet (added when the publishing workflow needs it). No code; the skeleton has not been imported into any Unity project.
- Order decided 2026-10-21 ([首輪範圍](plan.md#首輪範圍handler-與角色載入)): Handler validated with StreamingAssets, characters delivered as prefab + Animator Override Controller with the consumer's initialization adding the rest, Handler UnityEvents for consumer-defined loading performance; only then remote content tests; Git LFS separation and the Windows A/B/C avatar prototype after those.
- First consumer: VCP (`W:\UnityProject\Virtual-Chat-Partner`, Task ID `streaming-assets`), WebGL, character models. VCP is still waiting for art before its Cat delivery. VCP-specific stages stay in VCP's `Documentation/流式資源下載.md`; that document still describes L1 as "prefab unchanged, components stay on the prefab" and needs updating once the first-round discussion settles.
- Still proposals, not contracts: component/service/presentation responsibilities, the A/B/C avatar flow.

## Next steps

- Discuss [首輪待討論](plan.md#首輪vcp) with the user: performance hook (`AsyncTaskObject` on `Awaitable`), loading timing and UX alignment, where character-specific data lives, where to validate before VCP's delivery.
- Then implement the Handler in `Packages/Addressables/Runtime` and validate with a local group.
- Use [驗收清單](plan.md#驗收清單) to capture network/build-layout evidence as each prototype stage runs.

## Open decisions

- [首輪（VCP）](plan.md#首輪vcp): performance hook, loading timing, character-specific data, pre-delivery validation host.
- [頭像原型（遠端測試之後）](plan.md#頭像原型遠端測試之後): avatar overlay meaning, nonblocking menu entry.

## Ruled-out directions

- Separate local Asset Database runtime and remote production implementations: user explicitly wants one remote path.
- Git LFS URLs as runtime Addressables hosting: source assets require Unity content builds; R2 serves build outputs.
- Automatically preloading both permitted avatars before menu entry was replaced by initial A first and event-triggered B, to expose visible asynchronous work in the first validation case.
- Large models/scenes are deferred extensions; requiring them for the first proof would obscure the user's avatar-first learning objective.
- Putting the capability in `com.yu5h1.common` behind `versionDefines` (2026-10-21): the user treats `versionDefines` as a last resort (Animation uses it for Splines only because it had to); a package whose subject is Addressables content depends on Addressables directly.
- A Network package holding content loading and `AsyncTaskObject` (2026-10-21): Addressables content is not only network content, and `AsyncTaskObject` is not only used for network requests. Network stays a future category for communication only.
- Package names RemoteContent, CDN, Content (2026-10-21): "Remote" misses local groups; CDN names the hosting side, which the package never touches; UnityExtension names packages after the Unity feature they extend.
