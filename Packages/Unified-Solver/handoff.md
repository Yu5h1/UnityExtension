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

`Documentation/Plan/SolverParticle.md` is a **direction document for work not yet
started**: splitting today's `topology` enum into three axes — shape (CPU at
spawn), interaction (compute, every step), render (shader, every frame). Nothing
in it is scheduled or implemented. Read it before proposing changes to
`SolverParticleTopology`, `SolverShapeSource` or `SolverRenderProfile`, so the
same ground is not re-derived.

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

**Written, NOT compiled, NOT verified:**

- `SolverVolume` / `SolverVolumeEffectProfile` split and `SolverVolumeShape`
- `SolverBoundsProfile`, its lifecycle buffer and shrink fade
- `SolverMediumProfile.flowIsLocal`
- `tailBias`, the `_MediumState` buffer widening, the medium wake source

**Also not verified:** prefab apply/revert on the hidden companions; the removal
of `settleSpeed`.

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
- The reflection contract is **two** private fields: `SolverManager._rigidParticleRefCount`
  and `ClothGenerator._particleOffset`. It was three until rigid rendering stopped
  needing the buffers. No extension source touches the removed `RigidBodyBuffer`,
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

1. **Compile and verify the volume batch.** Everything under "Written, not
   compiled" is blocked behind one domain reload. Do this before adding to it.
2. **Excitement / startle.** Per-instance value scaling locomotion and decaying,
   raised by `Startle(origin, radius, amount)`. No spawning, no ParticleSystem:
   the bodies already exist, only their state changes. Jumping is not separate —
   it is a burst whose heading points up.
3. **Anisotropic drag.** Higher across the body than along it. Makes thrust emerge
   from the existing bend instead of being handed to the body, and aligns a slender
   body with the flow for free. Locomotion works without it; this makes it physical
   rather than asserted.
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
