---
name: web-replication
description: Implement a Yu5h1Lib.UX module (and Web-owned theme or config data) natively in Yu5h1Lib.UIToolkit — pin and import the UX spec data or Web delivery, implement to the UX spec, run the shared cases in Unity, report per spec item. Use when a UX module, control or foundation spec (for example Button) must exist natively in Unity; extracting UI out of a Unity product uses unity-ui-extraction, and WebGL pages that host Web components directly follow the parity plan's WebGL route.
---

# Web Replication

UnityExtension owns this skill. Direction, module status and conventions: [跨平台 UI 一致性](../../../Documentation/跨平台UI一致性.md). Follow the shared AgentsRule and the UX, Web and UnityExtension handoffs first.

## Sources

- Behavior: the module's UX spec, `UX/Documentation/ModuleSpec/[<layer>/]<Module>.md`. It is the only experience source; Web is another implementation.
- Data: module design data and cases from `UX/Documentation/ModuleSpec/[<layer>/]<Module>/` (pin the UX commit, dirty flag and SHA-256). Theme (`Web/dist/theme/theme.css`) and config declarations stay Web deliveries with their manifests until UX takes them over.
- Trigger: the user's 「在 Yu5h1Lib.UIToolkit 引用 UX 的 ##」, whose whole flow (inspect, implement the gaps, accept) is owned by the shared [referenced-module-acceptance](../../../../../.agents/skills/referenced-module-acceptance/SKILL.md) skill and uses this skill for the implementation steps; or a requester's `Requirements.UnityExtension.md` entry (Web for theme/config), whose Status belongs to the requester.
- Unity-side choices: `decisions/cross-platform-ui-parity.decisions`.

## Rules

- Never derive behavior from old Unity samples or from Web source code when the spec is silent. Raise the gap in `Documentation/Requirements.UX.md` (theme/config data: `Requirements.Web.md`) and wait for the spec.
- Web tests passing never counts as Unity verification. Verify every spec item in Unity.
- Physics goes through `Packages/Motion`; never re-implement a Motion solver in a UI module.

## Steps

1. **Pin.** Record the UX commit (or Web delivery for theme/config) and the versions/hashes being adopted in the module's acceptance record (step 4). Refuse an unknown major version or a dirty delivery unless the user accepts it.
2. **Import.** Run the Yu5h1Lib.UIToolkit Editor importer: validate everything before writing, generate USS variables (and WebTheme when Theme is active), copy cases into the package `Tests/`, write the imported-version stamp. On failure keep the previous output and report it as not updated. Re-importing the same input must produce no diff.
3. **Replicate.** Start from UI Toolkit's native components and USS, as referenced-module-acceptance requires; write code in `Runtime/WebUI/<Module>/` only for the rules they cannot express. When a module needs its own logic, write it by hand: a model whose commands match Web's (so cases run unchanged) and a control for input, capture and lifecycle. Follow the single-owner gesture rule in `Documentation/指標互動與世界橋接.md`.
4. **Verify.** Follow the shared [referenced-module-acceptance](../../../../../.agents/skills/referenced-module-acceptance/SKILL.md) skill: the record is `Packages/UIToolkit/Documentation/Yu5h1Lib.UX/<Module>.md`. Run the UX cases through the generic C# case runner; check each spec ID, including branches the cases do not cover; check target platforms. Never mark hand-feel or device input passed; the user checks them directly.
5. **Report.** Results, the pinned versions and platform differences go in that record only; update the handoff, and report to the requester when a `Requirements` entry triggered the work.

When Web's delivery changes, repeat from step 1; the stamp shows what is stale.
