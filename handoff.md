# UnityExtension Handoff

## Current Direction

Yu5h1's UnityExtension is a Unity application-extension solution from Yu5h1's development perspective. It collects reusable systems, tools, packages, and workflows so Unity projects can be built in a more efficient, low-waste, user-friendly way.

The root `plan.md` should stay concise and solution-level. Detailed architecture, internal systems, application domains, and developer operation guides should live under `Documentation` instead of bloating the root plan.

## Scope

Primary location: `C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension`

UnityExtension owns reusable Unity-facing packages and workflows. Application projects such as CombatAesthetic should be used to discover and validate reusable capabilities, not to own the shared architecture.

## Writing Guidance

- Do not lead plans with negative scope statements.
- Avoid design-pattern terminology in the root plan unless the user explicitly asks for it.
- Keep `plan.md` focused on what UnityExtension is trying to achieve.
- Put detailed system/application guides under `Documentation`.
- Preserve the user's language style for human-facing plans.

## Next Steps

1. Inventory existing packages and responsibilities.
2. Identify reusable capabilities currently trapped in application projects.
3. Plan package destinations for those reusable capabilities.
4. Add focused Documentation guides for important systems as they become clearer.
5. Execute the settled spec in [Component_Convergence](.agents/plans/Component_Convergence.md): `RendererAddon` owns material collection and instance lifetime, `MaterialController` binds `IReadOnlyList<Material>` sources and drives a `MaterialDriver` (the renamed `RendererMaterialResolver`, method `Drive`), `AssetSequence`/`MaterialSequence`/`RendererMaterialController` are deleted, material data moves to `MaterialArrayObject : ParameterCollection<Material>`, and the whole family lands in `common`. Migration scope is scanned: three mount points, all in the Unified-Solver test scene. The user will run this inside other refactor work rather than as a standalone task.
6. Verify whether `ParticleSystemRenderer` exposes its trail material through `sharedMaterials`. No longer an architecture blocker under the agreed direction, but it still decides how an instantiated trail material reaches `MaterialController`; the probe is recorded in the same plan.

## Recent Work

- 2026-08-18: Closed both open decision groups on the convergence spec. `MaterialResolver` becomes `MaterialDriver` with `Drive(IReadOnlyList<Material>)`, the three texture drivers rename with it, `fps` is extracted as a composable `FrameStepResolver` rather than a base class, and `MaterialController` loses its events, `SetXxxForShader`, and `Get` series. The `Aide` rename cleanup is now in scope as an independent step. Only two items still lack information: Unity's auto-instancing behavior and the particle-trail probe.
- 2026-08-18: Settled the convergence spec — sources are `IReadOnlyList<Material>`, `IEnumerator<Material>` was rejected, instance lifetime belongs to the supplier, and `AssetSequence`/`MaterialSequence` fold into the existing `ParameterCollection<Material>`. Recorded in [Component_Convergence](.agents/plans/Component_Convergence.md). No component reshaped yet.
- 2026-08-18: Renamed type `RendererAide` to `RendererAddon`. "Aide" is retired; extension components are uniformly named Addon. Other `Aide` types remain and are listed in the plan.
- 2026-08-18: Recorded the agreed convergence direction and the three open design questions in [Component_Convergence](.agents/plans/Component_Convergence.md). Still no existing component reshaped.
- 2026-08-17: Added `TextureScrollResolver` (`Packages/Animation/Runtime/Component/Resolver/`) for continuous UV offset scrolling — flowing water, beams, conveyors. Written without `[CreateAssetMenu]` per house convention, unlike its two older siblings. Not compile-verified.
- 2026-08-17: Opened [Component_Convergence](.agents/plans/Component_Convergence.md) to log the overlapping material-control components. No existing component was modified.

- 2026-07-27: Removed the `ParticleSystemRigidbody` implementation and its dedicated documentation. The Particle System C# Job approach remained CPU-bound and did not meet the required collision quality or performance for dense interactions.
- Decision: use `unified-solver` for large-scale collision, stacking, and container interactions. The retrospective is recorded in [DevelopmentLog.md](Documentation/DevelopmentLog.md).

