---
name: web-replication
description: Replicate a Yu5h1Lib.Web UI module in Yu5h1Lib.UIToolkit from Web's versioned delivery — pin and import the delivery, replicate to the Web spec, run Web's cases in Unity, report per spec item. Use when a Web module (AppsMenu, later theme or config) must exist natively in Unity; extracting UI out of a Unity product uses unity-ui-extraction, and WebGL pages that host Web components directly follow the parity plan's WebGL route.
---

# Web Replication

UnityExtension owns this skill. Direction, module status and conventions: [跨平台 UI 一致性](../../../Documentation/跨平台UI一致性.md). Follow the shared AgentsRule and both projects' handoffs first.

## Sources

- Behavior: the module's Web spec (for AppsMenu, `Web/Documentation/ModuleSpec/AppsMenu.md`, items U1–U8). It is the only behavior source.
- Data: `Web/dist/contracts/*` and `Web/dist/theme/theme.css`, with the delivery manifest (`delivery.json`, `<module>-delivery.json`): versions, source commit, dirty flag, SHA-256.
- Trigger: Web's `Requirements.UnityExtension.md` entry for the module. Its Status belongs to Web.
- Unity-side choices: `decisions/cross-platform-ui-parity.decisions`.

## Rules

- Never derive behavior from old Unity samples or from Web source code when the spec is silent. Raise the gap in `Documentation/Requirements.Web.md` and wait for the spec.
- Web tests passing never counts as Unity verification. Verify every spec item in Unity.
- Physics goes through `Packages/Motion`; never re-implement a Motion solver in a UI module.

## Steps

1. **Pin.** Record the Web commit and the manifest versions/hashes being adopted in the module's acceptance record (step 4). Refuse an unknown major version or a dirty delivery unless the user accepts it.
2. **Import.** Run the Yu5h1Lib.UIToolkit Editor importer: validate everything before writing, generate USS variables (and WebTheme when Theme is active), copy cases into the package `Tests/`, write the imported-version stamp. On failure keep the previous output and report it as not updated. Re-importing the same input must produce no diff.
3. **Replicate.** Write `Runtime/WebUI/<Module>/` by hand: a model whose commands match Web's (so cases run unchanged) and a control for input, capture and lifecycle. Follow the single-owner gesture rule in `Documentation/指標互動與世界橋接.md`.
4. **Verify.** Follow the shared [referenced-module-acceptance](../../../../../.agents/skills/referenced-module-acceptance/SKILL.md) skill: the record is `Packages/UIToolkit/Documentation/Yu5h1Lib.Web/<Module>.md` (AppsMenu's exists). Run Web's cases through the generic C# case runner; check each spec ID, including branches the cases do not cover; check target platforms. Hand-feel and device input are the user's checks — list them, do not mark them passed.
5. **Report.** Results, the pinned versions and platform differences go in that record only; update the handoff, then send the result to Web's requirement for Web to close.

When Web's delivery changes, repeat from step 1; the stamp shows what is stale.
