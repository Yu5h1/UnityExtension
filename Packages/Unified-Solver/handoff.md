# Yu5h1 Unified Solver Extension Handoff

## Scope

Repository: `Unity/UnityExtension`

Target: `Packages/Unified-Solver`

Canonical workspace: `C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension\Packages\Unified-Solver`

The former standalone Unified Solver workspace is retired and may be deleted. Do not route work or handoffs back to it.

The original `unified-solver` source is vendored as a read-only dependency under `Runtime/Dependencies/unified-solver` and `Editor/Dependencies/unified-solver`. Keep changes scoped to this package without modifying the vendored dependency unless the user explicitly requests it.

## Implemented

- `SolverParticleSpawnRequest`, `SolverParticleInstance`, topology/render enums.
- ScriptableObject profiles for particle topology, render data, oscillation and surface impulse.
- `SolverParticleEmitter` with append-only dynamic queue, safe pre-solver flush, capacity checks, topology builders for Single/Chain3/GuideChain4/DualRail6/RigidCluster4/ArticulatedCluster12, instance mapping buffer, scale and angular velocity support.
- `SolverMeshRenderer` with rigid and articulated procedural GPU rendering.
- `SolverParticleModifierRunner` plus batched `ApplyOscillation`, `ApplySurfaceImpulse` and `ApplyRollDamping` kernels.
- `Runtime/Shaders/SolverBodyFrameTypes.hlsl` and `SolverBodyFrame.hlsl` hold the single body-frame implementation. The modifier compute and `SolverArticulatedMesh.shader` both include them, so the plane the physics bends in and the plane the mesh is skinned in can no longer drift apart. They previously carried same-named `SideCandidate` functions with different DualRail6 sampling.
- Oscillation drives an angle-based C-bend: both segments rotate about the middle by +/- halfAngle, which sweeps head and tail through a real arc instead of projecting them back onto the current head-tail axis. `bendRatio` keeps its documented meaning through an `asin(2 * bendRatio)` mapping. The pose is built around the weighted centroid that the momentum balance uses, which removed GuideChain4's systematic `mΔ/4` sideways drift.
- `SolverOscillationProfile.vitality` caps the pose correction along the gravity axis, converted on the CPU from the solver's own `substeps`. Named for what it reads as on screen: 0 looks dead, higher looks fresher. Physically a launch-speed ceiling in m/s, and independent of `substeps`. See `plan.md` section 15.
- `SolverParticleProfile.rollDamping` plus `ApplyRollDamping` remove spin about the body's long axis with equal and opposite impulses about each control group's centre, so linear momentum is untouched. Structural rather than a modifier: it runs for every instance whether or not a modifier is attached. Control groups of one particle carry no roll information, so it has no effect on Chain3 or GuideChain4.
- `SolverSurfaceImpulseProfile` gates on the instance's mean velocity instead of a world-space height plane, so it works against cloth, colliders and piles rather than only a flat plane at a known Y. Superseded in practice by the contact reaction described in `plan.md` section 15.2; keep it disabled rather than deleted.
- `SolverParticleModifierProfile.enabled` gates dispatch per modifier without removing it from the profile's list.
- `SolverParticleEmitter.Awake()` adds a `SolverParticleModifierRunner` when the profile declares modifiers or roll damping and none is present, and logs a warning saying so.
- `ParticleSystemSolverBridge` with Trigger Enter batch conversion, local/world/custom simulation-space conversion, size/color/rotation/linear/angular velocity transfer, and accepted-only source particle removal.
- `SolverManagerAccess` compatibility bridge for the original solver's private rigid-body buffers, rigid-particle reference count, and `ClothGenerator` particle range. Rigid and cloth operations fail closed when their respective compatibility contract is unavailable.
- `ClothAnchor` and `ClothGrabber` resolve the original `ClothGenerator` range through the compatibility bridge; neither depends on the modified fork's `IsInitialized`, `ParticleOffset`, or `ParticleCount` properties.
- IL2CPP linker preservation for the reflected `SolverManager` and `ClothGenerator` fields.
- `Documentation/ParticleSystem x Unified Solver.md` defines ParticleSystem/Solver ownership boundaries, five cooperation modes, current component placement, phased validation, and long-term Soft Body goals without scheduling Soft Body implementation.
- README and architecture plan.

## Verification

- Runtime extension sources compile against the current original solver and Unity 6000.3.9f1 references with 0 warnings / 0 errors.
- Runtime compatibility tests compile with 0 warnings / 0 errors and cover field-contract resolution, rigid-particle reference count reads, pre-allocation buffer reads, and original `ClothGenerator` particle-range reads.
- User-confirmed Unity validation passes for the currently implemented extension features, including shader/compute import, particle profiles and topologies, rigid/articulated rendering, modifiers, ParticleSystem conversion, Solver colliders, cloth controls, Unity tests, and IL2CPP use.
- No extension source directly references the removed `RigidBodyBuffer`, `RigidParticleIndexBuffer`, or `RigidParticleRefCount` properties.
- No extension source references the modified fork-only `ClothGenerator.IsInitialized`, `ParticleOffset`, or `ParticleCount` properties.
- Original `SolverManager.cs` SHA-256 remains `4E902F723AF3B6C6D2640683A517340F24D12651BC328EBE49C5C24A27992483`.
- Original `ClothGenerator.cs` SHA-256 remains `EF927603C0D7A9A9B7A118FA7C0EBC4420AC02B6EB178548615F8142E744566B`.
- The validation assembly was generated directly from the current Runtime sources.
- No generic runtime class/shader/compute name contains Fish or Ice.
- User-confirmed in Unity: the shared body-frame headers compile; bending alternates between C and mirrored C; no corkscrew is observed; `enabled` gates a modifier; `vitality` controls how lively bodies look, with 0 reading as dead.
- Bodies bounce off surfaces from the contact reaction alone, with `SurfaceImpulse` disabled. Verified to still occur at `bendRatio = 0`, which isolates the source to the position channel because the velocity channel writes exactly zero at that value.

## Current quality gap

- Three longitudinal controls produce a C-bend and cannot produce an S-curve with an inflection point. Every existing topology carries exactly three spine controls: Chain3, GuideChain4, DualRail6 and ArticulatedCluster12 all have three. Off-spine particles supply the body frame and in-plane stiffness, not bending freedom. Adding particles without adding spine controls will not change this.
- A symmetric C-bend does not rotate the head-tail chord. That is correct, not a defect. A tail-dominant beat that yaws the body needs asymmetric drive, not more control points.
- `SolverOscillationProfile.acceleration` caps only the velocity channel, does not affect the pose, and never engages at its default: the drive needs about 1.75 m/s against a cap of `120 * 0.02 = 2.4`. It has to drop below roughly 87 to bind at all. Pending a user check of 0 against 120; delete it if there is no visible difference at 0.

## Solver behaviour that constrains any future work here

Established while building the bend and bounce. Read before tuning anything that touches forces, damping or contact. Full derivations in `plan.md` section 15.

- XPBD has no force API. Gravity in `Predict` is the only force and is a hardcoded uniform; there is no external-force buffer. Only two channels exist from outside: write `velocity` or write `position`.
- Velocity written from outside the substep loop is almost inert. `UpdateVelocity` rebuilds velocity from `(position - prevPosition)` every substep, so an injection survives one `Predict` out of `substeps`. Position writes are what take effect, and they bypass collision detection, so large ones tunnel.
- Bounce comes from the position write penetrating a support, the solver clamping it back, and `UpdateVelocity` dividing that correction by `subDt` rather than the frame. At 30 substeps that is roughly a 1500x amplification, so bounce is inversely proportional to `substeps`. `vitality` exists to cancel that coupling.
- `Particle` carries no contact flag, and no collision kernel writes state. Contact can only be inferred, and the instance mean velocity is the one signal immune to the momentum-neutral modifiers.
- Constraint damping is scaled by compliance: `gamma = compliance * damping / subDt`. `ClothGenerator.compliance` defaults to 0, which makes `constraintDamping` mathematically inert at any value.
- Global damping is a single uniform and `UpdateVelocity` never reads `phase`, so the solver has no per-group damping mechanism. Compensating from outside the loop does not work; it was tried and removed.

## Planned next work

- `plan.md` section 13 is now the design source for the P0 ice-fragment vertical slice: Editor source-Mesh fracture, serialized fragment bake data, non-degenerate 4/6/8-particle rigid groups, Runtime spawn/render mapping, fish/fragment/container collisions, and Sleep/Wake.
- Source-Mesh fracture is no longer deferred. It is the authoring entry point for the ice application. Runtime dynamic fracture and recursive re-fracture remain deferred.
- Sleep/Wake is required product behavior rather than an optional optimization. The initial extension state machine may stop visible jitter, but skipping Solver kernels requires explicit authority to modify the read-only vendored dependency or create a maintained fork.
- `plan.md` section 14.1 Muscle physics is demoted, not cancelled. Its original premise, that a kinematic drive can never produce a push-off reaction, is disproven: the contact reaction already launches bodies hard enough to need a cap. Muscle now buys control rather than capability. Re-read 14.1 before scheduling it.
- The vendored dependency stays unmodified. Forking was evaluated for per-group damping and is not needed, because non-zero `ClothGenerator.compliance` makes `constraintDamping` work. Sleep/Wake will force the decision back open.
- Copying `SolverManager` or `UnifiedSolver.compute` into this package was proposed and rejected. `SolverManager` has 36 private and 0 protected fields, so a subclass cannot reach the simulation loop; it would mean copying 1558 lines to change about 4, and the SHA-256 guarantees would hold in letter while certifying a file that no longer runs. If a solver change becomes necessary, a minimal in-place edit or a full fork both beat a partial copy.
- Any continuous modifier that writes motion, force, pose, or constraint targets must keep its instance awake and wake it immediately when enabled.
- `report.json` is the viewer-facing task projection for this plan. All fragment tasks remain planned and are not implemented yet.
- `Documentation/ParticleSystem x Unified Solver.md` is the lowest-priority long-term boundary document. The existing one-way bridge remains supported, but broader ParticleSystem integration must not block fragment physics.

## Known limitations

- Three longitudinal controls support C-bends but not true S-curves.
- `SolverParticleModifierRunner` is not added by `RequireComponent`: that attribute sits on the runner and pulls in an emitter, never the reverse. An emitter without it silently ran no modifiers and no roll damping, which cost a long debugging session because a fully configured profile is indistinguishable from one that runs and does nothing. `SolverParticleEmitter.Awake()` now adds it and warns. The same trap still applies to `SolverMeshRenderer`, which carries the same attribute direction and is not auto-added.
- Roll damping and the runner-added guard both depend on the runner existing on the same GameObject, so roll damping is still dispatched from a modifier component despite being a structural body property. Moving it into the emitter is unresolved.
- `vitality` limits only the component along the gravity axis, so bending across a surface is unrestricted. A body pressing into a wall or ceiling is not covered.
- The surface impulse's upward direction is hardcoded to the gravity axis and applied uniformly, so it produces pure translation with no torque and cannot make a body flip or curl off a surface.
- Global particle radius remains owned by the original solver.
- Instances are append-only; no free list/recycling yet.
- Cross-emitter capacity reservation is not globally atomic at the final capacity edge.
- Modifier dispatch is batched per emitter, not yet globally across all emitters sharing a modifier type.
- The non-invasive compatibility bridge depends on the names and types of three private `SolverManager` fields and the private `ClothGenerator._particleOffset` field.
- The original solver always performs synchronous rigid-body readback; the extension cannot disable it without changing the original source.
- Ice source-Mesh fracture, fragment bake assets, fragment runtime spawning, fragment collision validation, and fragment Sleep/Wake are planned but not implemented.
- A true GPU-saving Sleep path cannot be completed entirely in the extension while the vendored Solver Compute Pipeline remains read-only.
