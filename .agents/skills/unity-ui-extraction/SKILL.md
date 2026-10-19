---
name: unity-ui-extraction
description: Extract reusable UI capabilities from Unity consumer projects into UnityExtension packages, verify parity, and migrate callers. Use for Unity UI extraction or consolidation; replicating Yu5h1Lib.Web UI uses web-replication, and ordinary use of existing controls and extraction into the Web library follow their own routes.
---

# Unity UI Extraction

This skill belongs to UnityExtension; consumers reference it without copying it. Resolve UnityExtension as three directories above this file, independently of the caller's working directory. Follow the shared [AgentsRule](../../../../../.agents/AgentsRule.md) and each involved project's nearest handoff before work. User scope determines whether the task stops at a plan or includes implementation and adoption.

## Route the actual task

- Extract or consolidate Unity UI into shared packages: continue here, using [Unity knowledge](../../introduction.md) and its applicable conventions.
- Use an existing control: inspect its public API and owning documentation; do not invent an extraction. For preferences, Editor tooling, event wiring or serialization, load only the matching skill from the Unity knowledge entry.
- UI whose experience belongs in Yu5h1Lib.UX (a control, collection or module): the UX specification is the behavior source, not Web and not the Unity source. Integrate it with [cross-platform-module-spec](../../../../../.agents/skills/cross-platform-module-spec/SKILL.md), which takes this Unity source as its baseline, then implement it with [referenced-module-acceptance](../../../../../.agents/skills/referenced-module-acceptance/SKILL.md), using [web-replication](../web-replication/SKILL.md) for UI Toolkit. This skill still owns moving the Unity code into the package.
- Need Web changes: record the gap, with the Unity source as evidence, in the requesting project's `Requirements.Web.md` under the shared artifact rules. Authorized Web extraction uses [web-integration](../../../../../Web/.agents/skills/web-integration/SKILL.md), not a copy of this workflow. A shared specification does not make TS and C# one implementation.
- Non-UI parts found during extraction: inertia, springs and contact belong to the Motion project's decision ([inventory plan](../../../../../Motion/Documentation/InventoryPlan.md)); until it decides, keep the existing `common`/`animation` implementation in place. Other pure computation follows the owner rule below. Never name a Unity namespace `Yu5h1Lib.Physics` or `Yu5h1Lib.Motion`.

## Decide whether to extract

Extract a capability only when it already has a second user or a second implementation exists. Do not extract from source that is still changing: an interface pulled from moving code must be pulled again after every upstream change. Record such a candidate and its blocking condition instead.

## Identify the reusable owner

Inspect the real source and callers, not only a sample or plan. Trace assembly/package references, UXML/USS, scenes, Prefabs, ScriptableObjects, event subscriptions, resource loading and product data. Search existing packages before proposing a new control.

Read how each caller wires the capability, not only what to move. Write down the wiring rule every working caller obeys and put it in the package's documentation as a contract the next consumer follows. A rule all callers follow without stating it is the part an extraction most easily loses, and breaking it usually fails silently.

For each behavior classify the result as existing capability to reuse, host wiring, missing reusable mechanism, or retained product policy. Name its owning implementation and the smallest public seam, choosing the narrowest component that can own the rule without depending on the application layer; dependency checks confirm a destination is possible but do not choose it. Keep application orchestration outside the package; do not introduce product-name branches or let UI own a second copy of saved state.

Wiring that every consumer must write the same way, and that fails silently when written wrong, is a missing reusable mechanism even when no internal logic is copied. Policy stays injectable: a mechanism or module may supply defaults, but the consumer must be able to override them.

Examples that constrain extraction:

- Chat presentation can consume message snapshots and emit send/microphone requests; network, voice services and product flows stay with the consumer. Inspect [chat module](../../../Documentation/聊天面板模組.md) before making a second presenter.
- AppsMenu: the reusable parts are `GroupDragLayout`, `PointerVelocity`, `ElementTarget`, `Gesture.LongPress` and the Reaction/Transition motion; obstacles, item placement, drop judgment, product actions and fling threshold are policy with overridable defaults. Its pointer capture, release, capture-lost recovery and velocity sampling are the wiring consumers kept getting wrong; see [一條規則管所有手勢](../../../Documentation/指標互動與世界橋接.md#一條規則管所有手勢).

## Define the bounded extraction

Use the source project's existing plan and task route. Record source behavior/callers, the target package and dependency direction, what stays local, compatibility obligations, baseline evidence, smallest complete case, second use, and rollback. Apply the shared modular-design and artifact routes rather than creating a parallel planning format.

When the work needs a new package or a new package reference, make that the first step with no code moved: package resolution, asmdef references and Unity version are verified on their own, so a later failure can be told apart from an extraction failure.

The second use must change meaningful content or policy while consuming the same package implementation. A renamed copy of the first demo does not establish reuse. Preserve the source's settled semantics; distinguish a behavior-preserving extraction from an explicitly requested redesign.

Keep the probe and the second use versioned with the package, as a package sample (`Samples~/<Name>`), not only in a test project: the validation evidence must survive with the code it validates. Yu5h1LibTest has no version control.

When only a requirement is authorized, stop at the requesting project's requirement file. When implementation across repositories is authorized, make the package change and isolated consumer probe before production migration. Keep the source plan, library implementation status and requester acceptance in their respective owners.

## Preserve Unity references and lifecycle

Before moving assets, inspect `.meta` GUIDs, serialized type/field identities, assembly definitions, Prefab variants/overrides, persistent UnityEvent targets and UXML/USS/resource paths used by affected callers. Move the corresponding `.meta` with an asset when preserving identity; do not ship duplicate GUIDs. If a type, assembly, field or asset path must change, document and validate the required migration using the applicable serialization route rather than assuming a file move preserves every reference.

Keep runtime assemblies independent of Editor-only APIs and consumer assemblies. Follow the Unity knowledge entry for the real Core sources; backup folders are not implementation targets. Preserve attach/detach or enable/disable semantics, callback registration, scheduled work, cancellation and cleanup. Reopening or remounting a panel must not duplicate subscriptions or allow stale asynchronous results to update it.

Reuse current public package capabilities. Samples demonstrate composition and must not become an alternative runtime implementation. Keep a sample's single source in the package `Samples~/<Name>` with its `.meta` files; develop it in a test project through a directory junction from `Assets/Samples/<Package>/<Version>/<Display Name>` to that folder rather than a compiled `_dev` folder, which every consumer would compile and whose `Resources` would ship. Never Reimport or Remove a junctioned sample in Package Manager: it overwrites or deletes the package source. Avoid introducing a generic framework solely to relocate one product's view.

## Validate and adopt

Choose checks from the actual boundary changes:

- Compile in the real consumer/package setup; inspect affected scene/Prefab references and missing scripts/assets after import.
- Compare old/new observable behavior using the source baseline, including state, errors, cancellation and repeated attach/detach where relevant.
- Verify input, focus, IME, touch and visual behavior in the target environment when affected. Compilation or synthetic events do not prove device behavior; interaction feel and event timing need a hands-on rig.
- Exercise the second use through the same public package entry. If it requires copying internal logic or re-deriving the wiring rule, revise the boundary before declaring reuse.

Use existing validation scenes or hosts and the project's manual-test handoff rules. Record what was actually run, versions and limitations; do not launch broad tests or overwrite scenes merely to satisfy this skill.

After scoped checks pass, migrate the authorized consumer, verify its adopted path, and remove duplicated production logic or leave a delegating compatibility shim with an explicit retirement condition. Keep the previous task-owned path available until acceptance, without resetting unrelated changes. Library implementation, isolated probe success and production adoption are distinct outcomes.

Close through the owning handoff and requirement acceptance. Report the public entry, removed duplication, retained policy, reference/lifecycle evidence, second-use result and pending user checks. A preparation-only result remains preparation, not completed extraction.
