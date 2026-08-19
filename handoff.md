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
5. Compile the convergence change and confirm decision 7 in play mode: enter play, drive materials, exit, then verify no project material asset changed on disk (acceptance 8) and no material leaked (acceptance 3). Agent work stopped before this because building is the user's step.
6. Migrate the three mount points in `Packages/Unified-Solver/Runtime/Test/test solver mesh.unity` by hand (spec step 5.2, flagged irreversible). Two `RendererMaterialController` components (fileID 1698645394 on GameObject 1698645392, and 1978910209 on GameObject 1978910203) and one old-shape `MaterialController` (fileID 1698645395). Both controllers carry `resolver: {fileID: 0}` and the MaterialController's removed fields hold defaults, so nothing authored is lost - replace each with `RendererAddon` + `MaterialController` and wire the new `sources` array.
7. Confirm or reject the one remaining implementation deviation recorded under the plan's `已完成` section: decision 7's single runtime array is split into `sources`/`instances`/`materials`. Serialized fields still amount to `useSharedMaterial` alone. The other deviation was confirmed and extended into decision 9.
8. Step 7 of the spec (`Aide` cleanup) is untouched and independent. Its B5 item edits Core (`Unity/Runtime/Base/Source/Attribute/`) and needs explicit authorization first.


## Recent Work

- 2026-08-19: Decision 9 - the user generalized `RendererAddon` into `RendererAddon<TRenderer> : ComponentController<TRenderer>, IReadOnlyList<Material>` with `RendererAddon : RendererAddon<Renderer>` and `LineRendererAddon : RendererAddon<LineRenderer>, IColor`. Triggered by `TypeRestrictionDrawer` rejecting `LineRendererAddon` for `MaterialController.sources`; the agent had recommended a second component on the same GameObject instead and was overruled, so do not reopen it. Applied the two follow-ups that being a base class requires: `OnDestroy` is now `protected virtual` (a subclass declaring its own would hide the private one and silently leak every material copy, since Unity dispatches a message only to the most derived declaration), `RefreshMaterials` is `virtual`, and the full summary moved onto the generic with a one-liner left on the concrete class.
- 2026-08-19: Executed convergence spec steps 1-4 and 5.1. `MaterialArrayObject` replaces `AssetSequence`/`MaterialSequence`; `RendererAddon` rebuilt on decisions 7/8; `RendererMaterialResolver` moved to `common/Runtime/Component/MaterialDriver.cs` and the three texture resolvers to `common/Runtime/Component/Driver/Texture*Driver.cs`, all taking `IReadOnlyList<Material>` and stripped of `[CreateAssetMenu]`; new `FrameStepResolver` in `common/Runtime/Resolver/`; `MaterialController` rewritten around an `Object[]` source array plus an `[Inline] MaterialDriver`; `RendererMaterialController` deleted. Moves used `git mv` on the `.cs` and its `.meta` together, so script GUIDs survive. No compile has run - that is the user's step. Two deviations from the written spec are recorded in the plan and await confirmation.
- 2026-08-19: Closed the last two open items on the convergence spec. **C2 verified without launching Unity** - `m_Materials` is serialized, so reading `test solver mesh.unity` shows a trail-enabled ParticleSystemRenderer carries 2 materials with the trail at index 1; no second supplier type is needed. **C1 settled as decisions 7/8**: rather than reconciling with Unity's implicit instancing, `RendererAddon` stops calling `renderer.materials` altogether and instantiates its own materials, which collapses the proposed `owned` + `destroyInstancedMaterialsOnDestroy` + `useSharedMaterial` trio down to `useSharedMaterial` alone; `includeChildren` is deleted so one addon governs one Renderer. Corrected a wrong claim made earlier in the discussion: Unity does *not* auto-destroy instances created by `renderer.materials`, and C# GC never reclaims `UnityEngine.Object` - only `Resources.UnloadUnusedAssets()` does. The plan's unresolved list is now empty and the spec is fully executable.
- 2026-08-18: Closed both open decision groups on the convergence spec. `MaterialResolver` becomes `MaterialDriver` with `Drive(IReadOnlyList<Material>)`, the three texture drivers rename with it, `fps` is extracted as a composable `FrameStepResolver` rather than a base class, and `MaterialController` loses its events, `SetXxxForShader`, and `Get` series. The `Aide` rename cleanup is now in scope as an independent step. Only two items still lack information: Unity's auto-instancing behavior and the particle-trail probe.
- 2026-08-18: Settled the convergence spec — sources are `IReadOnlyList<Material>`, `IEnumerator<Material>` was rejected, instance lifetime belongs to the supplier, and `AssetSequence`/`MaterialSequence` fold into the existing `ParameterCollection<Material>`. Recorded in [Component_Convergence](.agents/plans/Component_Convergence.md). No component reshaped yet.
- 2026-08-18: Renamed type `RendererAide` to `RendererAddon`. "Aide" is retired; extension components are uniformly named Addon. Other `Aide` types remain and are listed in the plan.
- 2026-08-18: Recorded the agreed convergence direction and the three open design questions in [Component_Convergence](.agents/plans/Component_Convergence.md). Still no existing component reshaped.
- 2026-08-17: Added `TextureScrollResolver` (`Packages/Animation/Runtime/Component/Resolver/`) for continuous UV offset scrolling — flowing water, beams, conveyors. Written without `[CreateAssetMenu]` per house convention, unlike its two older siblings. Not compile-verified.
- 2026-08-17: Opened [Component_Convergence](.agents/plans/Component_Convergence.md) to log the overlapping material-control components. No existing component was modified.

- 2026-07-27: Removed the `ParticleSystemRigidbody` implementation and its dedicated documentation. The Particle System C# Job approach remained CPU-bound and did not meet the required collision quality or performance for dense interactions.
- Decision: use `unified-solver` for large-scale collision, stacking, and container interactions. The retrospective is recorded in [DevelopmentLog.md](Documentation/DevelopmentLog.md).

