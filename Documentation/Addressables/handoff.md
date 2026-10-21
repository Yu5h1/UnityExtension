# Addressables package handoff

## Scope and state

- Canonical design: [plan.md](plan.md). Authored 2026-09-09 as a learning plan under `Documentation/RemoteContent/`; renamed to `Documentation/Addressables/` on 2026-10-21 when the package was decided. No runtime code, asset repository, R2 bucket, workflow or package exists yet. Runtime outcomes in the acceptance list remain unverified.
- Package decided 2026-10-21: `Packages/Addressables`, `com.yu5h1.addressables`, namespace `Yu5h1Lib.AddressableAssets`, depends on common and `com.unity.addressables`. Rationale: [套件位置與依賴](plan.md#套件位置與依賴).
- First consumer: VCP (`W:\UnityProject\Virtual-Chat-Partner`, Task ID `streaming-assets`), WebGL, character models. VCP develops inside this package and references it; VCP-specific stages and acceptance stay in VCP's `Documentation/流式資源下載.md`. Design brought in from VCP: [第一個消費端：VCP](plan.md#第一個消費端vcpwebgl). VCP starts after its goal 1 (Cat delivery).
- Still proposals, not contracts: component/service/presentation responsibilities, the A/B/C avatar flow, Git LFS first.

## Next steps

- Settle [待討論決策](plan.md#待討論決策), first the conflict between this plan's Windows/LFS-first order and VCP as the first verification field; it decides whether work starts at VCP's L1a or at [學習與實作順序](plan.md#學習與實作順序) stage 1.
- When work starts, create the package skeleton (package.json, Runtime/Editor asmdefs, `.meta` files with fixed GUIDs), then add `com.yu5h1.addressables` to the consumer's manifest.
- Use [驗收清單](plan.md#驗收清單) to capture network/build-layout evidence as each prototype stage runs.

## Open decisions

- [待討論決策](plan.md#待討論決策): first verification field vs the LFS stage, avatar overlay meaning, nonblocking menu entry.

## Ruled-out directions

- Separate local Asset Database runtime and remote production implementations: user explicitly wants one remote path.
- Git LFS URLs as runtime Addressables hosting: source assets require Unity content builds; R2 serves build outputs.
- Automatically preloading both permitted avatars before menu entry was replaced by initial A first and event-triggered B, to expose visible asynchronous work in the first validation case.
- Large models/scenes are deferred extensions; requiring them for the first proof would obscure the user's avatar-first learning objective.
- Putting the capability in `com.yu5h1.common` behind `versionDefines` (2026-10-21): the user treats `versionDefines` as a last resort (Animation uses it for Splines only because it had to); a package whose subject is Addressables content depends on Addressables directly.
- A Network package holding content loading and `AsyncTaskObject` (2026-10-21): Addressables content is not only network content, and `AsyncTaskObject` is not only used for network requests. Network stays a future category for communication only.
- Package names RemoteContent, CDN, Content (2026-10-21): "Remote" misses local groups; CDN names the hosting side, which the package never touches; UnityExtension names packages after the Unity feature they extend.
