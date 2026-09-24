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

- Continue [專案開發系統分析技能計畫](Documentation/專案開發系統分析技能計畫.md#待討論決策): settle skill ownership, activation scope, decision flow, and a first consumer case. The draft records requirement classification, capability matching, and Prefab/data-driven authoring with editable results. No formal skill or AgentsRule change has been implemented.

- Continue [可變形物理世界與世界資產](Documentation/可變形物理世界與世界資產.md) from its world-creation premise: reduce dependence on individually finished, labor-intensive 3D assets. Unified-Solver's fish, bouncing, physical interaction and group-behavior work is exploratory experience, not a converged solution or selected backend. Identify the authoring burden before choosing a deformation prototype; existing PhysicsParticle contracts remain unchanged.

- Continue adoption and extension evaluation from [Agent friendly workflow](Documentation/Agent%20friendly%20workflow%20Yu5h1Lib.UnityExtension.md#待討論決策); locate the existing chat panel and custom UI for comparison.

- Validate [preferences](.agents/skills/preferences.md) on a real scene over MCP: its `execute_code` registration snippet has not been run yet. The defects in [偏好設定 — 已知問題](Documentation/偏好設定.md#已知問題) marked 推導 (E1/E2) come from reading the code and have not been reproduced.
- Continue [文字結構同步 — 待討論決策](Documentation/文字結構同步.md#待討論決策) to settle identity storage, completion-marker transport and omitted-field semantics before implementation. The user authorized the design document and report update; synchronization implementation has not started.

1. Decide whether `DraggableDesktop` should exist at all. The plan lists it for step 7, but there is no such control in HealthAI to port: the desktop lives inside `SignalClusterController` (843 lines) woven together with tiles, features, desktop modes, preference saving, hints, the Apps panel and the menu. Every reusable part of it is already extracted - `GroupDragLayout` holds the positions, group drag, inertia and obstacle avoidance; `Reaction.Throw` holds the fling; `PointerVelocity` and `SweptContact` are what its drag and hit tests call; `ElementTarget` bridges to elements. What is left is composition, and the user's own framing was that the Apps desktop is a consumer of the system rather than the system itself. Building the control anyway would mean inventing a shape no second consumer has tested. Recommend dropping it from the plan and recording why.

1. Verify the shared log through **HealthAI > Interaction Debug Window** and `W:\UnityProject\HealthAI\Assets\HealthAI\Editor\HealthAIInteractionDebugWindow.cs` in Unity: multi-line selection/copy/select-all, rejected edits, normal wheel scrolling, independent Ctrl+wheel zoom, Ctrl+middle-click reset to 12 pt, nested clipping, reflow, final-line scrolling, external foldout title, flat Clear toolbar, right-side search and its clear control, collapse retention and bottom-edge resizing with release outside the grip. The acceptance owner and integrated application caller are in `W:\UnityProject\HealthAI\docs\unity\Requirements.UnityExtension.md`; that project retains the Outstanding statuses until interactive acceptance passes.
1. Finish the BonghuoVR migration in `W:/UnityProject/BonghuoVR/Assets/StaticResources/`. `Torch Performance.prefab` carries a `RendererMaterialController` (`&615836047818230860`) whose type is deleted, so it shows as a missing script; its `resolver` pointed at `Animation/5x5 24fps.asset`. Replace it with a `MaterialController` whose `sources` is the `LineRendererAddon` already on that GameObject (that component is an `IReadOnlyList<Material>` since decision 9) and whose `driver` is that asset - the same shape `Sulfuric Fire Line.prefab` already has. Then set `5x5 24fps.asset` back to `fps: 24`: the field moved into `frameStep`, so re-saving silently drops it to the default 12 and halves the animation speed.
1. Inventory existing packages and responsibilities.
2. Identify reusable capabilities currently trapped in application projects.
3. Plan package destinations for those reusable capabilities.
4. Add focused Documentation guides for important systems as they become clearer.
5. Decide what to do about the duplicated Core sources. `TypeRestrictionAttribute.cs`, `ComponentController.cs`, `TypeRestrictionDrawer.cs`, `PlayerPrefsAdvanced.cs` and `ObservablePref.cs` each exist as two independent physical files - one under `Unity/Runtime` or `Unity/Editor`, one under `Unity/UnityExtension` - with identical content, different inodes, no symlink, and separate git repos. Every edit has to be written twice or the copies drift. Why they exist (presumably the csproj glob linking described in `.agents/build-notes.md`) was not traced.


## Recent Work

- 2026-09-16: Cleared the package-hygiene items the decoupling work had surfaced. Splines is now optional rather than undeclared: `Yu5h1Lib.Animation.asmdef` carries a `versionDefines` entry for `com.unity.splines`, and the two components that need it compile only under `YU5H1_SPLINES`. Verified by removing the package from the test project entirely - the animation assembly still builds and 180/180 EditMode tests pass, so an asmdef reference to an absent package's assembly does not break the assembly. The other ten components no longer drag Splines in.

  The Unity package-template leftovers are gone from `2D`, `Animation`, `InputSystem` and `Timeline`: test assemblies that named assemblies which never existed, the empty example tests they carried, and a `Samples/Example/SampleExample.cs` in each that belonged to no assembly and referenced a type that does not exist. All four packages can now be listed as testables.

  `Gesture.LongPress` is verified end to end - the 400ms trigger, `MoveTolerance` abandoning a wandering hold, `MoveTolerance = 0` refusing to, and the swallowed click were all exercised by hand in the Apps Menu sample, so that entry leaves Next Steps.

  The demo copy under `W:/UnityProject/Yu5h1LibTest/Assets/Demos` is deleted. `Packages/UIToolkit/Samples~/AppsMenu` is the single source; the test project imports it from the Package Manager when it is needed.


- 2026-09-16: `uitoolkit-decoupling` delivered steps 1-7. `com.yu5h1.uitoolkit` is new; `com.yu5h1.animation` gained the `Drive`, `Reaction` and `Transition` families; `com.yu5h1.common` gained the pointer geometry. EditMode 164/164 and PlayMode 9/9 pass, verified from inside the Editor over the MCP bridge rather than only in batchmode. HealthAI is untouched - not one line of its `Assets/` changed - and whether it adopts any of this is still its own decision.

  Design, boundaries and the verification record now live with the systems they describe: [運動系統](Documentation/運動系統.md) and [指標互動與世界橋接](Documentation/指標互動與世界橋接.md). The retrospective on how the consumer was built is in [DevelopmentLog](Documentation/DevelopmentLog.md). Those replace the eleven progress entries that used to sit here: they were keyed to the Yu5h1LibTest project, which has no version control, so the record would have gone with it.

  `Packages/UIToolkit/Samples~/AppsMenu` is the worked example and the hands-on rig. It settles `DraggableDesktop`: writing the consumer required inventing nothing, and everything left in it is policy, so the control should be dropped from the plan.

- 2026-09-09: Added the dedicated [文字結構同步](Documentation/文字結構同步.md) design and moved synchronization ownership out of the overall workflow draft. Updated the existing agent-scene-workflow report card; implementation remains planned. Both report schemas and overlay IDs passed validation. Independent document review clarified that pending Inspector writeback pauses the whole sync round. Viewer launch failed because the launcher could not access the stale localwebservice-8001.json state file under LocalAppData; visual verification remains unperformed.

- 2026-09-04: `ReadOnlyLogSection` keeps the toolbar above the messages, with native flat buttons on the left and a native search field on the right. Only the search field is bottom-aligned within the toolbar row, leaving its bottom border visible. Clear stays in its original position. The explicit top border is retained. Search filters literal, case-insensitive matching lines without changing caller data; changing the query resets the viewport. Existing caller signatures, compact padding, foldout, resize and Ctrl font controls are retained. The toolbar/search version compiled against Unity 6000.3.9f1 before the final top-border adjustment. Runtime filter probes failed to start with Windows access denied; the user reported Defender alerts and explicitly requested that testing be left to them. Do not repeat reflection probes or run additional tests for this UI task. The user confirmed the top-border fix visually. They clarified that only the search field should align to the toolbar bottom line; moving the entire toolbar was a misunderstanding and has been reverted. Search alignment is the latest source-only change; Unity verification is left to the user, as requested. Usage belongs to [.agents/skills/editor-tooling.md](.agents/skills/editor-tooling.md#read-only-log-panel).
- 2026-08-19: Corrected a scan recorded on 2026-08-18. It reported zero mount points under `W:/UnityProject/Assets`, but that path does not exist - W: holds one folder per project - so the scan read nothing and the absence was recorded as a finding. Re-scanned by script GUID: mount points exist in `W:/UnityProject/BonghuoVR`. Most were already migrated by the user; `Torch Performance.prefab` and `5x5 24fps.asset` still need hand work. The Yu5h1Lib half of that scan was correct and stands.
- 2026-08-19: Finished convergence spec step 7 and item B5, closing the plan entirely. `AudioSourceAide` and the `LayoutGroupAddon` log prefix were done by the user; `Collider2DAide` and its editor (which also carried the retired `Agent` word) renamed to Addon in the separate `Unity/CombatAesthetic` repo, `.meta` files moved with their sources so script GUIDs are unchanged. B5 documents on the `Mode` enum that `Exact` never matches an interface. Committing B5 revealed that the Core attribute file is duplicated rather than linked, so the note went into both repos - see next steps.
- 2026-08-19: Material convergence landed and is verified by the user - compiled, the three mount points in `test solver mesh.unity` migrated by hand, and a `TextureScrollDriver` asset wired into the surviving `MaterialController`. The `sources`/`instances`/`materials` split in `RendererAddon` was accepted as built, so no deviation is outstanding. Committed as `Converge material control into common`. The plan has no unresolved items left; only spec step 7 remains, and it is independent of materials.
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

## Open decisions

- [專案開發系統分析技能計畫 — 待討論決策](Documentation/專案開發系統分析技能計畫.md#待討論決策).

- [可變形物理世界與世界資產 — 待討論決策](Documentation/可變形物理世界與世界資產.md#待討論決策).

- [Agent friendly workflow — 待討論決策](Documentation/Agent%20friendly%20workflow%20Yu5h1Lib.UnityExtension.md#待討論決策).

- [文字結構同步 — 待討論決策](Documentation/文字結構同步.md#待討論決策).

- [偏好設定 — 待討論決策](Documentation/偏好設定.md#待討論決策).

## Ruled-out directions

- A standalone Log example window and menu were removed at the user's request: a dedicated permanent preview for this one control adds unnecessary UI. Keep the minimal API usage in the Editor tooling guide; verify through consuming panels, an existing shared UI viewer when available, or a temporary host.

- Raised-tab contours and mirrored toolbar backgrounds were retired: the user chose the standard flat Console toolbar with search. Retain an explicit top border for the embedded panel.
