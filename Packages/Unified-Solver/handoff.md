# Yu5h1 Unified Solver Extension Handoff

## Scope

Repository: `Unity/UnityExtension`

Target: `Packages/Unified-Solver`

Canonical workspace: `C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension\Packages\Unified-Solver`

Additional to the shared Unity index that `AgentsRule.md` step 4 already resolves:
`skills/gpu-rendering.md` is the one this package leans on hardest, and much of it
was written from this package's own work.

The former standalone Unified Solver workspace is retired and may be deleted. Do not route work or handoffs back to it.

The original `unified-solver` source is vendored as a read-only dependency under `Runtime/Dependencies/unified-solver` and `Editor/Dependencies/unified-solver`. Keep changes scoped to this package without modifying the vendored dependency unless the user explicitly requests it.

## Implemented

- `SolverParticleSpawnRequest`, `SolverParticleInstance`, topology/render enums.
- ScriptableObject profiles for particle topology, render data, oscillation and surface impulse.
- `SolverParticleEmitter` with append-only dynamic queue, safe pre-solver flush, capacity checks, topology builders for Single/Chain3/GuideChain4/DualRail6/RigidCluster4/ArticulatedCluster12, instance mapping buffer, scale and angular velocity support.
- `SolverMeshRenderer` with rigid and articulated procedural GPU rendering.
- `SolverParticleModifierRunner` plus batched `ApplyOscillation`, `ApplySurfaceImpulse` and `ApplyRollDamping` kernels.
- `Runtime/Shaders/SolverBodyFrameTypes.hlsl` and `SolverBodyFrame.hlsl` hold the single body-frame implementation. The modifier compute and `SolverArticulatedMesh.shader` both include them, so the plane the physics bends in and the plane the mesh is skinned in can no longer drift apart. They previously carried same-named `SideCandidate` functions with different DualRail6 sampling.
- Oscillation drives an angle-based C-bend: both segments rotate about the middle by +/- halfAngle, which sweeps head and tail through a real arc instead of projecting them back onto the current head-tail axis. The pose is built around the weighted centroid that the momentum balance uses, which removed GuideChain4's systematic `mΔ/4` sideways drift.
- Independent performance axes on `SolverOscillationProfile`, with `drive = vitality * (1 - stiffness) * playing` scaling both the pose and the velocity it injects. `plan.md` sections 15.3 and 15.5 are the reference.
  - `stiffness` is hardness and works in two opposing directions at once, which is what separates it from vitality: it converges particle velocities onto the instance mean, freezing whatever shape the body currently holds, and it lowers the drive. It carries no target shape of its own.
  - `vitality` is willingness to move. Both it at 0 and stiffness at 1 give `drive = 0`, but the first leaves the body limp for contact to reshape and the second locks the shape.
  - `muscleTension` replaces `bendRatio` and chooses the target shape through `asin(1 - muscleTension)`. 1 targets the topology's own rest form, the muscle-cramp behaviour. 0 is the geometric limit at 90 degrees rather than a natural amplitude; useful range is about 0.2 to 0.4.
  - `frequency` is how often a run begins and `duration` is how long one takes, authored directly in seconds. Duration also paces the pose, so a longer run is both slower and gentler on whatever the body rests against. Between runs the drive is released, leaving the body limp rather than straightened.
  - `tensionRandomness` and `durationRandomness` spread those per instance, alongside the existing `frequencyRandomness`.
- `SolverParticleProfile.rollDamping` and `settleSpeed` are structural: they run for every instance whether or not a modifier is attached. Roll damping removes angular velocity about the long axis, including a dedicated path for GuideChain4's guide particle. Settle converges relative velocity onto the instance mean once the body is barely deforming, fading in rather than switching at a threshold, so the body still travels while its shape stops drifting.
- `SolverSurfaceImpulseProfile` gates on the instance's mean velocity instead of a world-space height plane, so it works against cloth, colliders and piles rather than only a flat plane at a known Y. Superseded in practice by the contact reaction described in `plan.md` section 15.2; keep it disabled rather than deleted.
- `SolverParticleModifierProfile.enabled` gates dispatch per modifier without removing it from the profile's list.
- `SolverParticleEmitter.Awake()` adds a `SolverParticleModifierRunner` when the profile declares modifiers or roll damping and none is present, and logs a warning saying so.
- `ParticleSystemSolverBridge` with Trigger Enter batch conversion, local/world/custom simulation-space conversion, size/color/rotation/linear/angular velocity transfer, and accepted-only source particle removal.
- `SolverManagerAccess` compatibility bridge for the original solver's private rigid-body buffers, rigid-particle reference count, and `ClothGenerator` particle range. Rigid and cloth operations fail closed when their respective compatibility contract is unavailable.
- `ClothAnchor` and `ClothGrabber` resolve the original `ClothGenerator` range through the compatibility bridge; neither depends on the modified fork's `IsInitialized`, `ParticleOffset`, or `ParticleCount` properties.
- IL2CPP linker preservation for the reflected `SolverManager` and `ClothGenerator` fields.
- `Documentation/ParticleSystem x Unified Solver.md` defines ParticleSystem/Solver ownership boundaries, five cooperation modes, current component placement, phased validation, and long-term Soft Body goals without scheduling Soft Body implementation.
- README and architecture plan.

## Procedural ice fragments

The 4/6/8 rigid fragment slice from `plan.md` section 13.12. Fragments were
confirmed on screen in Unity; the move to instanced matrix rendering that
followed has not been compiled.

- `AddRigidBody` already sizes a group from `particleIndices.Length`, so 4/6/8
  physics needed zero vendored solver changes. The only thing pinning the
  extension to 4 was `_rigidIndexScratch = new int[4]`.
- New: `SolverShapeSource` (abstract seam), `SolverHullShapes` (base polyhedra
  and face tables), `SolverIceFragmentShapeSource`, `SolverHullMesh`.
- `SolverShapeSource` is the line that matters for the future. Baked fracture
  attaches as another subclass; without it the fracture pipeline would have to
  rewrite the spawn path rather than add to it.
- Degeneracy is prevented by construction rather than validated: a variant is a
  fixed combinatorial polyhedron with bounded vertex displacement, so a
  rank-deficient or self-intersecting fragment cannot be produced. This replaces
  `plan.md` 13.5's candidate-and-reject loop.
- Rigid instances are drawn with `Graphics.RenderMeshInstanced` and an ordinary
  material. `SolverManager.TryGetRigidBodyMeshPose` is public and the rigid body
  buffer is already read back every frame, so a rigid body is a mesh plus a
  matrix that costs nothing extra to obtain. `SolverRigidMesh.shader` is deleted:
  any URP or HDRP material now works unmodified, along with shadows, decals,
  motion vectors and the SRP batcher.
- The shape source hands out a fixed library of templates rather than a unique
  shape per instance, because `RenderMeshInstanced` takes one mesh and a matrix
  list; a shape nothing else shares cannot be batched. One instanced call per
  template. That is the batch-to-instance mapping `plan.md` 13.6 asks for, and
  fracture fits it exactly: a bake asset is already a fixed library.
- The matrix carries **no scale** on the hull path. Particle-radius inflation is
  baked into the template mesh in local space, and a scale would stretch it.
  Size variety therefore lives inside the templates, not in the instance.
- The reflection contract narrowed from three private `SolverManager` fields to
  one. `_rigidBodyBuffer` and `_rigidParticleIndexBuffer` were only read so a
  custom shader could place vertices; nothing needs them now.
- Capacity is reserved against `WorstCaseRequirements`, because the variant is
  not known until the shape source picks it and a half-reserved batch would
  leave partial instances behind.
- The ice profile must set `collideWithSameProfile = true`. Same non-zero phase
  means no collision, so leaving it false makes fragments pass through each
  other while still colliding with fish, which does not read as a phase problem.
- Sleep/Wake (`plan.md` 13.8) is not part of this and is still unstarted.

### Deleting a shader orphans materials in consuming projects

Removing `SolverRigidMesh.shader` left `GPU_Ice.mat` in BonghuoVR bound to a
missing shader, so nothing drew. The material still looked correctly assigned in
the inspector; the fault was one level below it. Before deleting a shader,
search the consuming projects for materials referencing its GUID, not just this
package for code references. `SolverMeshRenderer` now detects
`Hidden/InternalErrorShader` and names the material.

### First validation attempt failed, and why

Nothing was drawn. The cause was `SolverRenderProfile.hullFromParticles`, a bool
that defaulted off, had to be ticked by hand, and produced no geometry and no
error when it was not. Three design faults, all avoidable, all now fixed:

- `meshMode` and `hullFromParticles` restated what the particle profile already
  determines. Both are deleted. `SolverParticleProfile.MeshMode` and
  `.UsesHullRendering` derive them; a rigid profile with no authored Mesh draws
  its particle hull because that is the only thing it can draw. The existing ice
  and fish assets both reproduce their old behaviour with no edit.
- `SolverMeshRenderer` was not auto-added, so an emitter without one silently
  drew nothing. This exact trap is recorded under Known limitations and was
  still shipped. The emitter now carries `[RequireComponent]` for it; the pair is
  mutually required, which Unity allows.
- `playOnAwake` (was `spawnOnStart`) defaults true and `initialCount` defaults
  100. Defaults that produce nothing on screen make every other fault invisible.

The render profile now holds only genuine drawing choices. A shape source is not
a render profile and must not inherit one: it produces physics rest positions on
the CPU at spawn, and coupling them would stop one shape being drawn two ways or
one look being applied to two shapes.

## Companion components are owned, hidden, and drawn as modules

`SolverMeshRenderer` and `SolverParticleModifierRunner` are no longer the user's
to assemble. `SolverParticleEmitter.EnsureCompanions()` adds both from `Reset`,
`Awake` and the editor's `OnEnable`, sets `HideFlags.HideInInspector`, and the
emitter's `OnDestroy` removes them when the emitter is removed. The custom
editor draws them as foldout modules.

- This replaces `[RequireComponent]`, which was the wrong tool: it only turned
  "forgot to add it" into "forced to look at it". Unity's own ParticleSystem is
  also two components — `ParticleSystemRenderer` is real and separate — and is
  never experienced that way because its inspector draws the renderer as a
  module. Same technique.
- Both back-references to the emitter were removed. A `RequireComponent` from a
  hidden companion would block removing the emitter with a dialog naming a
  component the user cannot see.
- Companions are added unconditionally, not gated on what the profile looks like
  it needs. Gating meant that editing the profile later left the object one
  component short with nothing to say so.
- Cleanup is deferred through `EditorApplication.delayCall` and guarded on the
  GameObject still being alive, so closing a scene or leaving play mode does not
  trip it.
- Not yet exercised: prefab apply/revert and Undo on the hidden components.
  Check both before trusting this on prefabs.

Standing rule this came from: if code can settle it, code settles it. A setting
is for a real choice, not for restating something already determined elsewhere.
`hullFromParticles` and `meshMode` were the same fault in field form.

## Sleep holds settled bodies still; settle never could

Bodies kept creeping after landing. `settleSpeed` did not help, and the reason is
structural rather than a matter of tuning.

- `UnifiedSolver.compute` ends **every substep** with
  `p.velocity = (p.position - p.prevPosition) / subDt`, overwriting velocity
  outright. A modifier runs once per FixedUpdate, outside that loop, so a
  velocity it writes survives one Predict out of `substeps` — about 3% at 30.
  This was already recorded under solver behaviour; the earlier `settleSpeed`
  gate fix was necessary but nowhere near sufficient, and `settleSpeed` is now
  labelled weak in its own tooltip rather than left looking live.
- `ApplySleep` writes **positions**. Setting `prevPosition = position` is what
  makes a stop hold, because that difference is the only thing UpdateVelocity
  reads.
- Wake is by **displacement, not speed**. A sleeping body's velocity is held at
  zero by the kernel, so it cannot report its own motion; how far the solver
  managed to push it is the honest signal and covers every source at once.
- The runner is at `[DefaultExecutionOrder(50)]` and `SolverManager` declares
  none, so it sits at 0. Our kernels therefore run **after** the solver has
  stepped, which is what makes observe-then-correct work. Anything reordering
  those breaks sleep.
- An instance under any enabled modifier never sleeps (`_KeepAwake`), per
  `plan.md` 13.8.
- Extension-side only. It stops visible motion but still runs every solver
  kernel; skipping them needs the vendored dependency opened, which is a
  separate authorization.
- Buffers are sized to `maxInstances` so a slot always means one instance index,
  and are explicitly zeroed: undefined ComputeBuffer contents would read as
  instances already asleep at a pose made of whatever was in memory.

Settings that still make landing worse and are worth checking:

- `particleRadius` 0.1 against ice `baseDimensions` 0.12 x 0.4 x 0.08 means the
  collision shape is far larger than the authored size. Keep the smallest
  dimension comfortably above `2 * particleRadius` or lower the radius. This one
  also caps how small a crushed-ice chip can be.
- 100 instances in a 5 x 2 x 5 spawn volume average 0.79 m apart, which with the
  above overlaps at t=0; `maxDepenetrationSpeed` 5 m/s then throws them apart on
  the first frame.
- `frictionKinetic` 0.2 is genuinely slippery, right for ice and wrong for a pile
  that holds its shape. Friction is correct and substep-independent — the
  per-substep limit `mu * penetration` works out to `a = mu * g` — but it turns
  sliding into rolling, and a body made of spheres has no rolling resistance.

## Verification

- User-confirmed in Unity: fragments spawn at mixed 4/6/8 and are drawn through `Graphics.RenderMeshInstanced` with an ordinary URP material. The template library, the matrix path, the hidden companion components and the new spawn defaults all run. Switching `GPU_Ice.mat` from the deleted `SolverRigidMesh` shader to URP/Lit was the only change needed.
- NOT YET VERIFIED: the `settleSpeed` gate fix and the landing behaviour it was meant to cure; whether template repetition reads as repetitive at 24; prefab apply/revert and Undo on the hidden companions.
- User-confirmed in Unity: the section 15.11 hairpin fix compiles and resolves the fault. Head and tail no longer stick together in the net, and the spine no longer spins.
- Runtime extension sources compile against the current original solver and Unity 6000.3.9f1 references with 0 warnings / 0 errors.
- Runtime compatibility tests compile with 0 warnings / 0 errors and cover field-contract resolution, rigid-particle reference count reads, pre-allocation buffer reads, and original `ClothGenerator` particle-range reads.
- User-confirmed Unity validation passes for the currently implemented extension features, including shader/compute import, particle profiles and topologies, rigid/articulated rendering, modifiers, ParticleSystem conversion, Solver colliders, cloth controls, Unity tests, and IL2CPP use.
- No extension source directly references the removed `RigidBodyBuffer`, `RigidParticleIndexBuffer`, or `RigidParticleRefCount` properties.
- No extension source references the modified fork-only `ClothGenerator.IsInitialized`, `ParticleOffset`, or `ParticleCount` properties.
- Original `SolverManager.cs` SHA-256 remains `4E902F723AF3B6C6D2640683A517340F24D12651BC328EBE49C5C24A27992483`.
- Original `ClothGenerator.cs` SHA-256 remains `EF927603C0D7A9A9B7A118FA7C0EBC4420AC02B6EB178548615F8142E744566B`.
- The validation assembly was generated directly from the current Runtime sources.
- No generic runtime class/shader/compute name contains Fish or Ice.
- User-confirmed in Unity: the shared body-frame headers compile; bending alternates between C and mirrored C; `enabled` gates a modifier; `stiffness` at 1 freezes a body in its current shape while it still travels; `muscleTension` at 1 converges the body onto the topology's rest form; the three axes behave as specified in `plan.md` section 15.3.
- Bodies bounce off surfaces from the contact reaction alone, with `SurfaceImpulse` disabled. Verified to still occur with the bend amplitude at zero, which isolates the source to the position channel because the velocity channel writes exactly zero there.
- The fish profile in the consuming project uses `topology: 4`, which is GuideChain4. The roll damping kernel excluded that topology until recently, so `rollDamping` had never once run on it; a setting that appeared to do nothing was doing exactly what the gate said. Check the topology value before concluding a structural feature is broken.
- GuideChain4 has no resistance whatever to rotation about the spine, not merely a weak one: its three constraints on the guide all reach points on the spine, and rotating the guide about that line leaves every one of those distances unchanged. DualRail6's diagonals give a second-order restoring force, weak but non-zero, which matches the user's impression that it drifts less often.

## Fixed and verified: head and tail stuck together

Reported as a fatal glitch: the two ends meet, stay met, and the middle spine
plus the whole mesh spin fast enough that the user could not read the rate.
Common inside the net where bodies are squeezed against each other, rare on open
ground. Full detail in `plan.md` section 15.11.

- The sticking and the spinning are one fault. A hairpin has no body axis, so
  `middleTangent = normalize(headDirection + tailDirection)` — analytically
  `2*cos(halfAngle)*axis` — collapses to zero and, just before that, takes its
  direction from residual asymmetry amplified by `1 / (2*cos)`. The guard was
  `tangentLength > 1e-5`, absolute, on a vector whose natural scale is 2, so it
  never fired.
- Self-sustaining because `ApplyOscillation` builds its pose on that same axis.
  A reversed axis swaps the head and tail targets, the solver drags the body
  through, and the next frame measures the reversal again. It also explains the
  sticking: the drive does try to push the ends apart every frame, but in a
  direction that re-rolls each frame, so they random-walk instead of separating.
- Three layers, outermost first. `peakHalfAngle` capped at `MAXIMUM_HALF_ANGLE`
  70 degrees so the drive can never target the hairpin (`asin(1 - muscleTension)`
  asked for 90 at tension 0); `BisectDirections` judges the sum against its own
  scale of 2 and hands over near 160 degrees to an asymmetric, continuous chain
  direction that cannot invert; `UnfoldHairpin` holds the head-tail chord at a
  floor of 25% of body length, structurally, whatever the profile says.
- The unfold direction is the chord while the chord is meaningful, handing over
  to the body's own cross direction, made orthogonal to the chord first. Blending
  the two raw directions cancels them near a hairpin, where the chord is itself
  perpendicular to the spine and can sit anti-parallel to the cross direction.
- `SolverParticleModifierRunner` now dispatches the roll damping kernel
  unconditionally, because the unfold is structural. Roll damping and settle
  still gate themselves inside the kernel. `_OscillationUpAxis` became `_UpAxis`
  in shared parameters for the same reason.
- Tension above 0.06 is unaffected by the angle cap, so existing tuning stands.

## Open bug: mirrored body

A settled body occasionally mirrors end to end in a single step. The pose is correct; left and right swap, so the far side's texture shows. Full detail and the candidate fix are in `plan.md` section 15.10.

- `stiffness = 1` prevents it outright. Its only extra effect is converging particle velocities onto the instance mean, which removes relative motion.
- Setting `stiffness` to 1 and back to 0 at runtime clears the accumulation, and it stays clear for a while afterwards.
- Bodies at rest are fine. Bodies still slowly working their tail are the ones that flip.
- The drift accumulates at the position level inside the solver's substep loop, before any modifier can act, so velocity-side measures (`rollDamping`, `settleSpeed`) only slow it. `settleSpeed` acts near stillness while the fault occurs during slow motion, so the two ranges miss each other.
- A per-instance side memory buffer was proposed to undo sub-threshold rotation and rejected as the wrong direction. Recorded as a fallback, not scheduled.

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
- `torsionAlign` was added and then removed: it solved nothing, because the hourglass it was aimed at turned out to be a shader-side sign flip, and whole-body roll leaves the segments agreeing with each other so it cannot detect that either. It also caused jumping and a standing oscillation of its own. See `plan.md` section 15.8 before proposing it again.
- `SolverParticleModifierRunner` is not added by `RequireComponent`: that attribute sits on the runner and pulls in an emitter, never the reverse. An emitter without it silently ran no modifiers and no roll damping, which cost a long debugging session because a fully configured profile is indistinguishable from one that runs and does nothing. `SolverParticleEmitter.Awake()` now adds it and warns. `SolverMeshRenderer` had the identical trap and it was left unfixed long enough to cost a failed validation pass; the emitter now declares `[RequireComponent(typeof(SolverMeshRenderer))]`. Check for this attribute direction on any new companion component before assuming it is reachable.
- Roll damping and the runner-added guard both depend on the runner existing on the same GameObject, so roll damping is still dispatched from a modifier component despite being a structural body property. Moving it into the emitter is unresolved.
- The bounce budget limits only the component along the gravity axis, so bending across a surface is unrestricted. A body pressing into a wall or ceiling is not covered.
- Apparent bend speed is `peakHalfAngle * angularFrequency`, so amplitude and rate both read as speed and cannot be separated in a continuous wave. `muscleTension` at 0 gives 90 degrees, roughly double the amplitude the retired `bendRatio` default produced, which reads as frantic. A `burstDuration` split was tried and reverted for not addressing this; see `plan.md` section 15.8.
- `muscleTension` at 0 maps to the geometric limit rather than a natural amplitude, which contradicts reading 0 as the relaxed resting state. Partly resolved: the limit is now clamped to 70 degrees rather than 90, so 0 no longer targets the head-onto-tail fold, but the bottom of the range is still an angle limit rather than a natural amplitude, and 0.2 to 0.4 remains the working range.
- The surface impulse's upward direction is hardcoded to the gravity axis and applied uniformly, so it produces pure translation with no torque and cannot make a body flip or curl off a surface.
- Global particle radius remains owned by the original solver.
- Instances are append-only; no free list/recycling yet.
- Cross-emitter capacity reservation is not globally atomic at the final capacity edge.
- Modifier dispatch is batched per emitter, not yet globally across all emitters sharing a modifier type.
- The non-invasive compatibility bridge depends on the name and type of one private `SolverManager` field, `_rigidParticleRefCount`, and the private `ClothGenerator._particleOffset` field. It was three until rigid rendering stopped needing the buffers.
- The original solver always performs synchronous rigid-body readback; the extension cannot disable it without changing the original source.
- Ice source-Mesh fracture, fragment bake assets, fragment runtime spawning, fragment collision validation, and fragment Sleep/Wake are planned but not implemented.
- A true GPU-saving Sleep path cannot be completed entirely in the extension while the vendored Solver Compute Pipeline remains read-only.
