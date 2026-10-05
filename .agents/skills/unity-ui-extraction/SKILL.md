---
name: unity-ui-extraction
description: Extract reusable UI capabilities from Unity consumer projects into UnityExtension packages, verify parity, and migrate callers. Use for Unity UI extraction or consolidation; ordinary use of existing controls and extraction into the Web library follow their own routes.
---

# Unity UI Extraction

This skill belongs to UnityExtension; consumers reference it without copying it. Resolve UnityExtension as three directories above this file, independently of the caller's working directory. Follow the shared [AgentsRule](../../../../../.agents/AgentsRule.md) and each involved project's nearest handoff before work. User scope determines whether the task stops at a plan or includes implementation and adoption.

## Route the actual task

- Extract or consolidate Unity UI into shared packages: continue here, using [Unity knowledge](../../introduction.md) and its applicable conventions.
- Use an existing control: inspect its public API and owning documentation; do not invent an extraction. For preferences, Editor tooling, event wiring or serialization, load only the matching skill from the Unity knowledge entry.
- Align Unity and Web behavior: read [cross-platform UI plan](../../../Documentation/跨平台UI一致性.md). It owns shared-contract boundaries and pilot gates; this skill owns the Unity extraction method.
- Need Web changes: record the gap in the requesting project's `Requirements.Web.md` under the shared artifact rules. Authorized Web extraction uses [web-integration](../../../../../Web/.agents/skills/web-integration/SKILL.md), not a copy of this workflow. A shared specification does not make TS and C# one implementation.

## Identify the reusable owner

Inspect the real source and callers, not only a sample or plan. Trace assembly/package references, UXML/USS, scenes, Prefabs, ScriptableObjects, event subscriptions, resource loading and product data. Search existing packages before proposing a new control.

For each behavior classify the result as existing capability to reuse, host wiring, missing reusable mechanism, or retained product policy. Name its owning implementation and the smallest public seam. Keep application orchestration outside the package; do not introduce product-name branches or let UI own a second copy of saved state.

Examples that constrain extraction:

- Chat presentation can consume message snapshots and emit send/microphone requests; network, voice services and product flows stay with the host. Inspect [chat module](../../../Documentation/聊天面板模組.md) before making a second presenter.
- AppsMenu is composition. Inspect the existing drag/gesture/motion primitives and the [ruled-out directions](../../../handoff.md#ruled-out-directions) before proposing a desktop control. Obstacle selection, item ownership and drop policy may remain host responsibilities even when their mechanics are reusable.
- Presentation controls such as settings panels do not depend directly on Preferences or choose persistence policy. Consumers compose the connection. The existing PreferencesBinder is a dedicated integration layer within UI Toolkit and may use IPreferences; this does not mean it already supports isolated manual drafts, cancellation or apply-only retry. Inspect the [preferences design](../../../Documentation/偏好設定.md) and actual binder behavior before adding coordination; preserve the functional-package/settings-object boundary defined there.

## Define the bounded extraction

Use the source project's existing plan and task route. Record source behavior/callers, the target package and dependency direction, what stays local, compatibility obligations, baseline evidence, smallest complete case, second use, and rollback. Apply the shared modular-design and artifact routes rather than creating a parallel planning format.

The second use must change meaningful content or policy while consuming the same package implementation. A renamed copy of the first demo does not establish reuse. Preserve the source's settled semantics; distinguish a behavior-preserving extraction from an explicitly requested redesign.

When only a requirement is authorized, stop at the requesting project's requirement file. When implementation across repositories is authorized, make the package change and isolated consumer probe before production migration. Keep the source plan, library implementation status and requester acceptance in their respective owners.

## Preserve Unity references and lifecycle

Before moving assets, inspect `.meta` GUIDs, serialized type/field identities, assembly definitions, Prefab variants/overrides, persistent UnityEvent targets and UXML/USS/resource paths used by affected callers. Move the corresponding `.meta` with an asset when preserving identity; do not ship duplicate GUIDs. If a type, assembly, field or asset path must change, document and validate the required migration using the applicable serialization route rather than assuming a file move preserves every reference.

Keep runtime assemblies independent of Editor-only APIs and consumer assemblies. Follow the Unity knowledge entry for the real Core sources; backup folders are not implementation targets. Preserve attach/detach or enable/disable semantics, callback registration, scheduled work, cancellation and cleanup. Reopening or remounting a panel must not duplicate subscriptions or allow stale asynchronous results to update it.

Reuse current public package capabilities. Samples demonstrate composition and must not become an alternative runtime implementation. Avoid introducing a generic framework solely to relocate one product's view.

## Validate and adopt

Choose checks from the actual boundary changes:

- Compile in the real consumer/package setup; inspect affected scene/Prefab references and missing scripts/assets after import.
- Compare old/new observable behavior using the source baseline, including state, errors, cancellation and repeated attach/detach where relevant.
- Verify input, focus, IME, touch and visual behavior in the target environment when affected. Compilation or synthetic events do not prove device behavior.
- Exercise the second use through the same public package entry. If it requires copying internal logic, revise the boundary before declaring reuse.

Use existing validation scenes or hosts and the project's manual-test handoff rules. Record what was actually run, versions and limitations; do not launch broad tests or overwrite scenes merely to satisfy this skill.

After scoped checks pass, migrate the authorized consumer, verify its adopted path, and remove duplicated production logic or leave a delegating compatibility shim with an explicit retirement condition. Keep the previous task-owned path available until acceptance, without resetting unrelated changes. Library implementation, isolated probe success and production adoption are distinct outcomes.

Close through the owning handoff and requirement acceptance. Report the public entry, removed duplication, retained host policy, reference/lifecycle evidence, second-use result and pending user checks. A preparation-only result remains preparation, not completed extraction.
