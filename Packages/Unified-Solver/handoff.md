# Yu5h1 Unified Solver Extension Handoff

Live state only. **Why anything is the way it is → `plan.md`. How to assemble a
scene → `.agents/skills/unified-solver.md`.** If you find yourself explaining a
design here, it belongs in one of those two.

## Scope

Repository: `Unity/UnityExtension` · Target: `Packages/Unified-Solver`

Canonical workspace:
`C:\Users\Yu5h1\Dev\VSProjects\Yu5h1Lib\Unity\UnityExtension\Packages\Unified-Solver`

Additional to the shared Unity index that `AgentsRule.md` step 4 already
resolves: `skills/gpu-rendering.md` is the one this package leans on hardest, and
much of it was written from this package's own work.

The original `unified-solver` is vendored read-only under
`Runtime/Dependencies/unified-solver` and `Editor/Dependencies/unified-solver`.
Keep changes out of it unless the user explicitly authorizes otherwise. The
former standalone Unified Solver workspace is retired; do not route work back to
it.

## Where the reasoning lives

| Subject | `plan.md` |
|---|---|
| Ice fragment P0, procedural fragments | 13.1–13.12 |
| Sleep/Wake design intent | 13.8 · implementation findings in 21 |
| Medium: density, flow, waterline, no swimming | 13.13 |
| Muscle physics (demoted, not cancelled) | 14.1 |
| Bend drive, bounce, performance axes, tail bias | 15.1–15.12 |
| Mirrored body (open) · hairpin (fixed) | 15.10 · 15.11 |
| Volume/effect split, migration, medium wake channel | 16 |
| Bounds effect | 17 |
| Locomotion and steering | 18 |
| speedLimit | 19 |
| Jet as a medium, `flowIsLocal` | 20 |

`Documentation/ParticleSystem x Unified Solver.md` owns the ParticleSystem
boundary and is lowest priority. `report.json` is the viewer-facing projection.

`Documentation/Plan/PhysicsParticle.md` is a **complete, agreed direction for
work not yet authorized or started**: replacing the old emitter-centred authoring
with an always batch-capable, backend-neutral `PhysicsParticle` driver containing
layout, interaction and appearance profiles, integrated first through a shared
Unified Solver backend component. Nothing in it is scheduled or implemented.
Read it before proposing changes to
`SolverParticleTopology`, `SolverShapeSource` or `SolverRenderProfile`, so the
same ground is not re-derived.

`Documentation/Plan/PhysicsParticleAnchor.md` is a second, **separate** agreed
direction, and it is the one **authorized to go first**: `ClothAnchor` →
`PhysicsParticleAnchor`, where one Transform binds many particles each carrying a
captured local offset, behind an `IPhysicsParticleSource` seam so the same
component serves `ClothGenerator` and `RopeGenerator`. It borrows
`PhysicsParticle.md`'s naming and backend-neutral boundary but does not depend on
that work, which stays unstarted. Read it before touching `ClothAnchor`; see open
problem 7.

Only after the user explicitly authorizes implementation, its next step is:

- Translate the agreed direction into the first implementation
  checklist and settle the exact public Core / backend APIs.

## Build state

**Compiled and user-confirmed in Unity:** procedural 4/6/8 ice fragments through
`Graphics.RenderMeshInstanced` with an ordinary URP material; hidden companion
components with Undo/Redo; Sleep stopping settled bodies; template repetition at
24 unnoticeable; medium volumes floating and dragging bodies; locomotion moving a
group toward a target; steering pointing heads along the heading and keeping
bodies upright; `flipForward` after its half-turn fix; a sleeping body carried off
by a current; the shared body-frame headers; the three performance axes of
`plan.md` 15.3; the 15.11 hairpin fix; bodies bouncing off surfaces from contact
reaction alone with `SurfaceImpulse` disabled.

Added to that list this session, all user-confirmed in Unity: the
`SolverVolume` / `SolverVolumeEffectProfile` split; `flowIsLocal`;
`PhysicsParticleAnchor` with `KeyValues` bindings and its scene-view editor;
rope bend constraints; the shape contract end to end — `PrimitiveShape`, a
`ParticleSystemAddon` cone driving a region, and live retuning of
`bendStiffness` while playing.

**Medium parameters are now physical. Written this session, NOT compiled.**
`SolverMediumProfile.viscosity` is **deleted**; a medium carries only `density`
and `flow`. The coupling coefficient moved to the body as
`SolverParticleProfile.dragCoefficient`, and locomotion gained
`SolverLocomotionProfile.mediumThrust`. Both multiply the **medium-to-body
density ratio** the kernel already computes for buoyancy, not the raw authored
density — that is in profile-mass units where neutral lands in the hundreds. A
neutral medium gives a ratio of 1, so both defaults of 1 reproduce the old
`viscosity = 1`. No GPU stride changed: the coefficient took
`SolverParticleInstance._padding` and the purchase took the unused
`_MediumState.w`. Design and rejected alternatives in `PhysicsParticle.md` §9.9,
behaviour in `plan.md` §18.2.

**`density = 0` no longer works and should not be reintroduced.** A ratio of 0
neither drags nor gives anything to push off. The `splash water` cone is now
authored as what it physically is — spray is dispersed water, so a density well
below the still body it came from, with a large `flow`. Bodies are swept and
cannot swim against it, and both follow from the description rather than from a
setting chosen to produce them. User-confirmed in Unity.

The authoring stance this settled: **match the direction, not the figure.**
Density here is in profile-mass units and never will be kg/m³, so the question is
"denser or thinner than this body, and roughly by how much" — still water dense,
spray thin, air far thinner. Recorded in `.agents/skills/unified-solver.md`.

**Written, NOT compiled, NOT verified:**

- `SolverBoundsProfile`, its lifecycle buffer and shrink fade
- `tailBias`, the `_MediumState` buffer widening, the medium wake source
- `SolverClothParticles.FindExistingTransformAnchors` — compiled, not run
- `ShapeKind.Cylinder` — the kernel branch exists, nothing has used it

**Also not verified:** prefab apply/revert on the hidden companions; the removal
of `settleSpeed`; non-uniform-scale refusal on a `ParticleSystemAddon`.

## Anchors: ClothAnchor → PhysicsParticleAnchor

**Working in Unity.** Design and rationale are in
`Documentation/Plan/PhysicsParticleAnchor.md`.

The editor slowness was diagnosed before rewriting, and the cause was worse than
the guess in the plan: `OnSceneGUI` issued **one `Handles.Button` per grid node**
— 2500 control IDs, hit tests and draw calls on a 50×50 cloth — plus 2500
`GetHandleSize` camera projections, ~5000 more matrix ops rebuilding the grid
line arrays, and a fresh `Dictionary`, **every repaint, including mouse move**.
`AddAnchor` also created **one GameObject per anchored vertex**.

The rewrite removes all four:

- Rest positions and grid line arrays are cached, keyed on particle count,
  `LayoutSize` and the source's `localToWorldMatrix`.
- Picking is one control ID plus a nearest-particle search **run on mouse events
  only**; a repaint does no picking work.
- Dots are drawn for anchored and hovered particles only — the grid lines already
  mark every other one.
- One Transform per binding, not per vertex.

**Bindings are a `KeyValues<Transform, List<Point>>`, not a `Binding[]`** (user
decision). `KeyValuesDrawer` draws the whole map in the inspector, so the editor
hand-rolled no list UI at all; `PhysicsParticleAnchorEditor` derives from
`Yu5h1Lib.EditorExtension.Editor<PhysicsParticleAnchor>` and its
`OnInspectorGUI` is `base.OnInspectorGUI()` plus an Edit toggle, an *Editing*
Transform field, and Recapture Offsets. Scene-view picking is unchanged. Edits
go through `Undo.RegisterCompleteObjectUndo` on the component, not through
`SerializedProperty`.

**Shape contract, and the start of the neutral core's own folder.** Working in
Unity: a `ParticleSystemAddon` cone drives a region, confirmed through fish
locomotion inside it, and `flowIsLocal` resolves against the provider's frame.

- `Runtime/ParticlePhysics/` is the backend-neutral core that is expected to
  leave this package eventually, as `com.yu5h1.particle-physics`.
  `IPhysicsParticleSource` and `PhysicsParticleAnchor` moved there (with their
  `.meta` files, so `pip.prefab` still resolves).
- **Namespace `Yu5h1Lib.ParticlePhysics`** — the two words are deliberately in
  that order. `PhysicsParticle` is itself a planned component, and a namespace
  sharing a type's name produces CS0118-class ambiguity; the namespace names the
  domain, the types name the things in it. `Yu5h1Lib.Physics` was rejected: an
  enclosing namespace beats a `using`, so it would shadow `UnityEngine.Physics`
  in **every** `Yu5h1Lib.*` file — five in the library already use bare
  `Physics.` — and in any consumer with `using Yu5h1Lib;`. `Physics` is also
  already taken as a folder name in this library, for Unity-physics helpers.
- **The whole package moved from `Yu5h1.UnifiedSolver` to
  `Yu5h1Lib.UnifiedSolver`.** There is no `Yu5h1` namespace in this library;
  the old one was wrong. 24 files.
- New in `common`: `ShapeKind`, `IShapeProvider`, `PrimitiveShape`,
  `ShapeGizmos`, `ParticleSystemAddon`. They live in `common` and not here
  because `ParticleSystemAddon` is in `common` and the dependency only runs one
  way.
- **`SolverVolume` no longer has geometry of its own.** One provider, no
  fallback to its own Transform — a fallback is what makes "dragging this
  object does nothing" silent. `OnValidate` adds a `PrimitiveShape` through
  `delayCall` and carries the old `shape` enum into it, so old scenes migrate
  without losing anything.
- **A cone's axis is +Z and its two radii ride on X and Y.** `SolverVolumeGPU`
  is 96 bytes with every float spoken for, and a circular cone uses only two of
  three axes, so `Size.x` is the wide diameter, `Size.y` the narrow one (zero =
  true cone, non-zero = frustum) and `Size.z` the length. The axis is +Z so a
  provider never converts between frames — an earlier +Y version cost two axis
  bugs and was reverted. Recorded in `ShapeKind` and in `VolumeReaches`.
- **Anything meaning "in the volume's own axes" must read `SolverVolume.Rotation`,
  not `transform.rotation`.** `SolverMediumProfile`'s `flowIsLocal` was the one
  place still reading the Transform, which is identical until a provider sits on
  another GameObject.
- `SolverVolume` has a **Log Uploaded Values** context menu that runs the
  runner's own packing and prints the resolved entry. It is what ended a run of
  guesses about a flow that appeared to do nothing.
- `VolumeReaches` gained `Cylinder` and `Cone` branches, and an unknown id now
  reaches nothing rather than falling through to the sphere.

Recorded in `Documentation/Plan/PhysicsParticle.md` §9.7, with the namespace
decision in §9.1. Two statements elsewhere in that document went stale and were
corrected: the §9.3 tree said geometry came from the Transform, and §9.6's third
reason for deferring the `SolverMotionTarget` merge — that a field had no sphere
— no longer holds.

Renaming `SolverVolume` to `PhysicsField` stays blocked on payload neutrality
(§9.1) and is its own task, together with the effect-family rename.

Two design conclusions were reached in discussion and recorded rather than
built. Neither is scheduled:

- **`plan.md` §18.2** — what the three medium parameters actually do today, and
  the one interaction between them: `density` is buoyancy only, `viscosity` is
  what sweeps a body, and terminal rise speed is buoyant acceleration over
  viscosity. Whether a body holds against a current is a plain speed comparison.
- **`PhysicsParticle.md` §9.9** — the direction that replaces it. Buoyancy, drag
  and thrust all scale with the same density, so a medium should carry only
  `density` and `flow`; the drag and thrust coefficients belong to the **body**,
  which is where `viscosity` is misplaced today. Propulsion is two coefficients,
  not a mode flag, because a duck both swims and walks. The price is that every
  existing medium needs retuning; the reward is that the pipe stops needing a
  physically impossible `density = 0`.
- **`PhysicsParticle.md` §9.8** — how the environment itself is represented.
  Today an analytic field; possibly a sampled two-way texture next; and beyond
  that environment particles, for the three things a height field cannot hold —
  overhangs and caves, breaking waves, underwater vortices. Explicitly labelled
  as exploration with no design and no cost estimate.

**`SolverParticleSource` owns re-applying solver settings.** Solver data is
written once at spawn and then lives on the GPU, so editing an authoring field
afterwards changed nothing — the user confirmed this for `RopeGenerator`'s own
`compliance` and `constraintDamping`. The base now carries `_settingsDirty`,
set from `OnEnable` and `OnValidate`, consumed on `FixedUpdate` at
`[DefaultExecutionOrder(-50)]` once the particle range resolves, and handed to a
virtual `ApplySolverSettings(manager, offset, count)`. It also owns the delayed
"still not applied after 250 steps — <reason>" warning, since every subclass has
the same three silent failure modes. **Still open:** the vendored generators'
own constraints cannot be retuned this way — nothing knows their constraint
indices. Reaching them needs either a full `ConstraintBuffer` readback and scan,
or a fourth and fifth reflected field (`_constraints`, `_constraintsDirty`),
which would grow the documented contract. Not decided.

It is a plain `[Range(0f, 1f)] float`, not an `Optional<float>`: zero already
means no bend, and **an attribute drawer beats a type drawer**, so `[Range]` on
an `Optional<>` bypasses `OptionalDrawer` entirely and Unity's `RangeDrawer`
prints "Use Range with float or int." That is a library-wide trap, not a
Unified-Solver one.

**`SolverRopeParticles.bendStiffness` is a 0..1 stiffness, not compliance.** It
was authored as compliance first and that was wrong twice over: the scale runs
backwards (0 = rigid) and its useful values sit between 1e-8 and 1e-4, so the
whole range collapsed into "0 works, everything else does nothing". It is now
the fraction of a rigid correction applied per step, and `ComplianceFor` solves
`wSum / (wSum + alphaTilde)` backwards for the compliance that delivers it,
using the live substep dt and particle mass — so the feel survives changing
either.

**`SolverRopeParticles` can add bend constraints** (`bendStiffness`, an
`Optional<float>`, off by default). `RopeGenerator` connects i to i+1 only,
which resists stretch and nothing else — a chain of such constraints has
literally zero bend resistance, so curvature collapses into one joint and the
rope reads as two rigid bars with a kink. A second distance constraint spanning
i to i+2 fixes it, and one uniform compliance is enough for "stiff near the
anchor, drooping at the tip" because cantilever moment grows with length
squared. No vendored file was touched: `SolverManager.AddDistanceConstraint` is
public and `_constraintsDirty` re-uploads, so constraints can be added long
after spawn. It retries from `FixedUpdate` at
`[DefaultExecutionOrder(-50)]` rather than running once from `Start`, so it
depends on no ordering against `RopeGenerator` and still lands in the same
physics step the solver uploads dirty constraints in (`SolverManager` steps in
`FixedUpdate`, SolverManager.cs:604/612). Waiting costs nothing because
`SolverManager` never reads its CPU particle array back from the GPU — the
captured rest length is always the pristine `2 * spacing`. Adding constraints is one-way — `SolverManager` has no
remove — but the **values are live-tunable**: the constraints are contiguous, so
`SyncBend` rewrites this rope's slice of the public `ConstraintBuffer` with
`SetData` whenever `bendStiffness` or `bendDamping` changes in play mode, and
writes `restLength = -1` (the kernel's broken marker) when the toggle goes off.
No reflection and no vendored change: `DistanceConstraintGPU` is public and the
vendored code shares this assembly. The CPU-side list still holds the original
values, so anything that dirties constraints re-uploads over the patch; both
sources (`AddDistanceConstraint`, `ResetSimulation`) move `ConstraintCount`,
which is what the re-patch check watches. Its default is `1e-6`, not the
`0.001` first suggested: compliance is divided by the **substep** dt squared, so
0.001 applies under 0.1% of a rigid correction and does nothing visible. Scale
recorded in `.agents/skills/unified-solver.md`.

**The glue adds itself.** `PhysicsParticleAnchorEditor` hooks
`ObjectFactory.componentWasAdded` (public in 6000.3.9f1, verified against the
assembly) and, deferred through `delayCall`, adds `SolverClothParticles` or
`SolverRopeParticles` and assigns `Source` as soon as the GameObject says which
one it needs. It fires for the generator and the glue as well as the anchor, so
the parts can arrive in any order. It never guesses: with neither generator
present it does nothing, because both glue components carry `[RequireComponent]`
for their generator and a guess would fabricate a body. The button is now only
for anchors that already exist without a Source — scenes upgraded from
`ClothAnchor`, which the hook cannot see. All of it is deletable in one file
when `PhysicsParticle.md` removes the glue.

Three consequences worth knowing:

- **This package now depends on `com.yu5h1.common`.** `FX.UnifiedSolver.asmdef`
  had no references at all; both asmdefs and `package.json` gained it.
- **`KeyValues.OnAfterDeserialize` deletes entries whose key is null** when
  `TKey` is a class (`Packages/common/Runtime/Data/KeyValues.cs:255`). With a
  `Transform` key that means a row added through the list's `+` button and left
  unassigned vanishes on the next reload. The scene-view flow never creates one
  — the entry appears when a particle is bound to the chosen Transform — and the
  inspector warns when one exists. No existing `KeyValues` user has an Object
  key, so this had never been hit before.
- **`FixedUpdate` no longer walks the dictionary.** `Entries`, `Keys`, `Values`
  and the enumerator all allocate; `_groups` is a flat cache of
  (Transform, List) rebuilt only when the set of Transforms changes.
  `InvalidateBindings()` is the manual door for runtime mutation.

New files: `Runtime/Core/IPhysicsParticleSource.cs`,
`Runtime/Components/SolverParticleSource.cs` (abstract backend half),
`SolverClothParticles.cs`, `SolverRopeParticles.cs`.
`ClothAnchor.cs`, `ClothAnchorEditor.cs` and `ClothAnchor.compute` were **`git mv`'d**
to their new names so GUIDs survive; `PhysicsParticleAnchor` carries
`[MovedFrom(… "ClothAnchor")]`.

- **A particle's identity is a flat `int` index into its own body, never a
  coordinate.** An earlier draft used a `Vector2Int` selector; it forced a rope
  to pretend to be `(segments, 1)` and **could not address a 3D lattice at all**,
  which is exactly where `PhysicsParticle.md` is heading. `LayoutSize`
  (`Vector3Int`, zero when not a lattice) survives for **display only** — grid
  lines and `(x, y)` labels — so an unstructured point cloud still works and
  simply draws as points.
- **The core is backend neutral.** `PhysicsParticleAnchor` produces
  `(index, world position)` and nothing else. `SolverManager`, the particle
  buffer, the compute dispatch and the reflection bridge all live behind
  `IPhysicsParticleSource`. The auto-add helper for the glue is in the **editor**
  for the same reason — the component must not reach for a Solver type.
- **`PhysicsParticleAnchor.compute` is unchanged apart from its name.** Offsets
  are applied CPU-side, so the GPU struct is still `(index, position)`.
- **Migration is automatic but partial.** The old `anchors` array is kept as a
  hidden field with its original name and layout, and `OnValidate` folds it into
  `bindings` grouped by Transform — flattening the old `(x, y)` node through the
  source's row width, which is why migration **waits until a Source is assigned**
  rather than guessing a width and clearing the legacy field unrecoverably — with
  **every offset left at zero**, which is exactly what the old component did. An upgraded scene still needs its
  `SolverClothParticles` added and `Source` assigned; the inspector offers both
  as buttons when `Source` is empty.
- **Anchoring sets `invMass = 0` and never restores it** (unchanged behaviour).
  Unbinding at runtime therefore leaves the particle pinned. `ClothGrabber`
  restores inverse mass on release; the anchor does not, because an anchor is
  permanent by intent.

Execution order is unchanged at `-100`: an anchor writes where a particle *is*,
before `SolverManager` steps, which is the opposite of the modifier runner at
`+50`.

**Removed by user decision — do not rebuild it.** A `Carry` effect (target
velocity read from the volume's own motion) was half-built and is deleted, along
with `SolverVolumeEffectType.Carry` and `SolverVolume.LinearVelocity`, which had
no other consumer. Transport with an **authored** direction — conveyor, current,
jet — is a medium with `flow` plus `flowIsLocal`, and that covers the cases this
project has. Only a scoop or a hand needs the target read from the object, which
is not needed here.

The diagnosis that motivated it is still true and still worth knowing:
**`SolveBox` friction cannot carry anything.** It measures
`p.position - p.prevPosition`, the particle's own world displacement, so the
collider's motion never enters the calculation — a resting particle on a sliding
top face has zero displacement and gets zero correction. An animated collider
bulldozes with its walls and drags nothing with its floor. Recorded in
`.agents/skills/unified-solver.md` so it is not rediscovered as a bug.

Had Carry been finished as designed it would also have failed on a settled load:
`ApplySleep` erases the velocity channel, and the wake channel (`_MediumState.y/.z`)
is written only by the medium branch, so a sleeping fragment in a scoop would
never have been carried at all.

## Contract guarantees

- Original `SolverManager.cs` SHA-256 remains
  `4E902F723AF3B6C6D2640683A517340F24D12651BC328EBE49C5C24A27992483`.
- Original `ClothGenerator.cs` SHA-256 remains
  `EF927603C0D7A9A9B7A118FA7C0EBC4420AC02B6EB178548615F8142E744566B`.
- The reflection contract is **three** private fields:
  `SolverManager._rigidParticleRefCount`, `ClothGenerator._particleOffset` and
  `RopeGenerator._particleOffset` (added with rope anchoring). `RopeContractAvailable`
  is deliberately **outside** `ContractAvailable`, so a scene with no rope is not
  told the whole bridge is broken. No extension source touches the removed `RigidBodyBuffer`,
  `RigidParticleIndexBuffer`, `RigidParticleRefCount`, or the fork-only
  `ClothGenerator.IsInitialized` / `ParticleOffset` / `ParticleCount`.
- Runtime sources and compatibility tests compile against the current original
  solver and Unity 6000.3.9f1 with 0 warnings / 0 errors. Tests cover field-contract
  resolution, rigid-particle reference counts, pre-allocation buffer reads, and
  original `ClothGenerator` particle-range reads.
- No generic runtime class/shader/compute name contains Fish or Ice.
- IL2CPP linker preservation is in place for the reflected fields.

## Open problems

1. **Mirrored body.** A settled body occasionally mirrors end to end in one step;
   pose correct, left and right swapped. `stiffness = 1` prevents it outright.
   Bodies still slowly working their tail are the ones that flip. Accumulates at
   the position level inside the substep loop, so velocity-side measures cannot
   reach it. Full detail and the rejected side-cache fallback: `plan.md` 15.10.
   **Do not reintroduce a third settle control** — `settleSpeed` was removed for
   writing velocities that contact overwrites in the same substep.
2. **Body density varies by variant.** `particleMass = profile.mass / particleCount`,
   so a 4-particle fragment is twice as dense as an 8-particle one from the same
   profile: big ice floats while small ice sinks. Mass semantics are per profile,
   not per part. Blocks a literally heavy head too.
3. **Buoyancy may not lift a body off the floor.** Contact rebuilds velocity from
   position in the same substep. Untested; most likely place the medium design
   disappoints. The lift term at least wakes such a body.
4. **Bounds gaps:** no fade on the rigid path (CPU matrices, GPU lifecycle); a
   fading body still collides at full particle radius; an emitter spawn box outside
   the boundary recycles forever with nothing checking it.
5. **Three longitudinal controls cannot produce an S-curve.** Every topology
   carries exactly three spine controls. Adding particles without adding spine
   controls will not change this.
6. **`SolverOscillationProfile.acceleration` never engages at its default.** The
   drive needs ~1.75 m/s against a cap of `120 * 0.02 = 2.4`; it has to drop below
   roughly 87 to bind. Pending a user check of 0 against 120 — delete it if there
   is no visible difference.

## Next, in order

1. **Verify what is left of the volume batch.** `SolverBoundsProfile` and
   `tailBias` are the remainder; the rest of that batch is confirmed. Do this
   before adding to it.
2. **Excitement / startle.** Per-instance value scaling locomotion and decaying,
   raised by `Startle(origin, radius, amount)`. No spawning, no ParticleSystem:
   the bodies already exist, only their state changes. Jumping is not separate —
   it is a burst whose heading points up.
3. **Anisotropic drag.** Higher across the body than along it. Makes thrust emerge
   from the existing bend instead of being handed to the body, and aligns a slender
   body with the flow for free. Locomotion works without it; this makes it physical
   rather than asserted. Its framing — that swimming and flying are one mechanism
   and only walking is a second — is in `plan.md` §18.2, together with the density
   term locomotion still lacks.
4. **Mass semantics for shape sources.** Open problem 2. Either mass becomes per
   particle or it scales with count.
5. **Split `SolverParticleModifiers.compute` per kernel.** 1900 lines holding seven
   kernels read one at a time, so every task pays for all seven. Shared declarations
   and the `SolverMedium` / `SolverMotionTarget` structs move to an `.hlsl` beside
   the body-frame headers; each kernel gets its own `.compute` that includes it. The
   runner resolves kernels by name, so it grows one `ComputeShader` reference and
   nothing else changes. **Do this before the per-instance state buffers are merged**,
   not after, or the two refactors collide.
6. **Ground locomotion.** Walking pushes on a surface, not a medium, and contact
   rebuilds velocity from position, so the velocity channel cannot carry it. Needs
   a position-channel design.
7. **Editor source-Mesh fracture and fragment bake assets.** `plan.md` 13.4. The
   authoring entry point for the ice application. Runtime dynamic fracture and
   recursive re-fracture stay deferred.

Standing constraints on all of the above:

- The vendored dependency stays unmodified. Forking was evaluated for per-group
  damping and is not needed. A true GPU-saving Sleep path will force the decision
  back open; copying `SolverManager` or `UnifiedSolver.compute` into this package
  was proposed and rejected (36 private, 0 protected fields — 1558 lines copied to
  change about 4, and the SHA-256 guarantees would certify a file that no longer
  runs).
- Any continuous modifier that writes motion, force, pose or constraint targets
  must keep its instance awake and wake it immediately when enabled.
- `SolverMeshRenderer` and `SolverParticleModifierRunner` are owned, hidden and
  drawn as modules by the emitter. The pattern itself is in
  `.agents/skills/editor-tooling.md`; do not re-derive it here.

## Known limitations

- Instances are append-only; no free list or recycling.
- Cross-emitter capacity reservation is not globally atomic at the final capacity edge.
- Modifier dispatch is batched per emitter, not globally across emitters sharing a
  modifier type.
- Each emitter's runner uploads the whole volume list separately — correct, but
  redundant with several emitters.
- Global particle radius is owned by the original solver, which also always
  performs synchronous rigid-body readback; the extension cannot disable it.
- Roll damping is structural but still dispatched from the modifier runner, so it
  depends on that component existing. Moving it into the emitter is unresolved.
- The bounce budget limits only the component along the gravity axis, so a body
  pressing into a wall or ceiling is not covered.
- The surface impulse's up direction is hardcoded to the gravity axis and applied
  uniformly, so it produces pure translation and cannot make a body flip or curl
  off a surface. Superseded in practice by contact reaction (`plan.md` 15.2); keep
  it disabled rather than deleted.
- Apparent bend speed is `peakHalfAngle * angularFrequency`, so amplitude and rate
  cannot be separated in a continuous wave. `muscleTension` 0 maps to the 70° limit
  rather than a natural amplitude; working range is 0.2 to 0.4.
- `torsionAlign` was added and removed; see `plan.md` 15.8 before proposing it again.
- GuideChain4 has **no** resistance to rotation about the spine — its three
  constraints on the guide all reach points on the spine, so rotating the guide
  leaves every distance unchanged. DualRail6's diagonals give a weak second-order
  restoring force. Check the topology value before concluding a structural feature
  is broken: the fish profile uses `topology: 4` (GuideChain4), and the roll damping
  kernel excluded it until recently.
