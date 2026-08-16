# Unified Solver: applying it

Use this skill to set up or change behaviour in `Packages/Unified-Solver`.

Assembly procedure only. Live state and open problems are in that package's
`handoff.md`; the reasoning behind each design is in its `plan.md`. Read those
for *what is true now* and *why it is this way* — this is *what to attach to
what*. If this file and the code disagree, the code is right and this is stale.

## Scene says where, asset says how

Getting this backwards is the usual first mistake. A profile is a
ScriptableObject and can hold no position; a component has a Transform and holds
no tuning.

| Scene component | Asset it points at |
|---|---|
| `SolverParticleEmitter` — spawns instances | `SolverParticleProfile` |
| `SolverVolume` — where a region is | `SolverVolumeEffectProfile[]` |
| `SolverMotionTarget` — where bodies head | — |
| `SolverBoxCollider` *(vendored)* — what blocks | — |

If a feature seems to need both a position and tuning, it wants two objects.

## Volumes carry a list of effects

`SolverVolume` is geometry only: shape (Box or Ellipsoid) from the Transform,
plus an `effects` list. One box can be water inside **and** recycle anything
leaving, without two components duplicating the same geometry and inside test.

Every effect carries `actOutside`, because that motivating case needs both sides
of one surface at once.

| Effect | Granularity | Does |
|---|---|---|
| `SolverMediumProfile` | Particle | density, flow, viscosity |
| `SolverBoundsProfile` | Instance | fade out, move back, fade in |

Granularity is not a performance note. **Per particle** is what gives a floating
body its waterline for nothing: part of it is inside, part is not, and it settles
at partial submersion with nothing modelling a surface. **Per instance** is for
decisions only meaningful about a whole body — half a fish cannot be recycled.

**A moving `SolverBoxCollider` does not carry its load.** `SolveBox` measures
friction against the particle's own world displacement, so the box's motion never
enters it: a resting particle on a sliding top face has zero displacement, gets
zero friction correction, and the box slides out from under it. A conveyor or a
current is a **medium** with `flow` and `flowIsLocal`, not an animated collider.

Adding an effect is an enum value, a subclass with `Write`, and a kernel branch.
It touches neither `SolverVolume` nor the upload path. That is the seam.

## Minimum working scene

```
SolverManager   <- vendored; gravity, substeps, particleRadius, friction
Water           <- SolverVolume (Box) -> [SolverMediumProfile]
Fish            <- SolverParticleEmitter -> SolverParticleProfile
Heading         <- SolverMotionTarget
```

The emitter creates and hides `SolverMeshRenderer` and
`SolverParticleModifierRunner` itself. Do not add them by hand and do not expect
to see them: the emitter's inspector draws them as modules.

## Order of operations, and why it matters

`SolverManager` declares no execution order, so it sits at 0. The emitter is at
−100 and the runner at 50, so every kernel runs **after** the solver has stepped,
observing what it just did and correcting before the next `Predict`. Reordering
these breaks Sleep, Bounds and steering.

Within the runner: speed limit → upload volumes → medium → roll damping →
modifiers → bounds → sleep. Bounds is after the modifiers so a teleport is the
last positional word of the step; sleep is after so a driven body reads as moving.

**Registering solver data is on the same clock as stepping it.** `SolverManager`
steps in `FixedUpdate` and uploads whatever is dirty at the top of that step, so
`AddParticle`, `AddDistanceConstraint` and their kin belong in `FixedUpdate` too.
Doing it from `Update` compiles, runs, and looks correct — it just guarantees the
body is simulated for at least one step without whatever was being added, because
every `FixedUpdate` in a frame has already run by the time `Update` is reached.
The symptom is a single wrong-looking first step, which is exactly the kind of
thing that gets blamed on the parameter that was being registered.

This is one-time setup, not simulation, which is what makes `Update` look
reasonable. It is not: what decides the clock is which data is touched, not
whether the work repeats.

## The two channels

Only two ways to affect the simulation from outside. Picking wrong is the most
common way to build something that does nothing.

- **Velocity** survives on a **free** body, and is erased wherever a contact, a
  constraint or rigid shape matching writes the position in the same substep.
  Use for buoyancy, flow, locomotion, steering, speed limiting.
- **Position** always holds, but only if `prevPosition` is written to match. The
  solver rebuilds velocity from their difference, so an unmatched position write
  becomes velocity divided by the **substep** — roughly a 1500× amplifier. Use
  for Sleep, Bounds, and anything that must stop or move a resting body.

A control that damps a settled body through the velocity channel cannot work.
That has been built and removed twice here.

**Nothing damps a vendored generator's rope or cloth except the global
`SolverManager.damping`.** Constraint damping enters as
`gamma = compliance * beta / subDt`, so a constraint stiff enough to be worth
having has a compliance near zero and can carry no damping at all — and even
when it can, it damps the rate the constraint is violated, never a whole body
swinging as a pendulum with every constraint satisfied. `speedLimit` does not
reach these bodies either: it lives in the extension's modifier runner, which
only walks emitter instances. Global `damping` applies
`v *= 1 - damping * subDt` per substep, which integrates to roughly
`e^(-damping * t)` — so the number is the reciprocal of a decay time constant,
and it is scene-wide.

## Where the cost actually is

Computing is cheap. **Moving data between CPU and GPU is slow, and waiting for it
is the expensive thing.** Judge any addition here by asking whether it makes
something stand at that door once per frame.

- Particles live in a `ComputeBuffer`. Modifiers read, modify and write that same
  buffer in place, and the renderer reads it too, so nothing round-trips. Keep it
  that way: a modifier written in C# would have to pull the buffer back.
- The **rigid** path does round-trip — the CPU builds the matrices for
  `RenderMeshInstanced`, and the vendored solver's rigid-body readback is
  synchronous. The **articulated** path does not, because the shader reads the
  buffer directly. That makes a fish cheaper per instance than a fragment despite
  being the more complex body.
- Instance count within one emitter is **not** a cost to worry about: every
  modifier is one dispatch for all of them. 100 and 1000 cost the same in
  dispatches. What multiplies is the **number of emitters** — roughly eight
  dispatches each per FixedUpdate, plus a redundant copy of the whole volume
  upload per runner.

## One thread is one instance, and that has a ceiling

Every kernel here is dispatched as `ceil(instanceCount / 64)` groups, and each
thread loops over its own instance's particles **serially**.

This is a design decision, not an oversight: threads cannot communicate, and a
momentum-neutral drive has to subtract the mean over *its own body*. One thread
owning one whole body is what makes that mean available with no coordination.

The ceiling follows directly. At 4 particles per instance the loop is optimal. At
a few thousand — a lattice body, a cloth-sized soft body — one thread runs
thousands of iterations while the other 63 in its group idle. Kernels that are
genuinely **per particle** (the medium first) would have to be re-dispatched per
particle with an instance lookup before this package can carry bodies that large.
See `Documentation/Plan/SolverParticle.md`.

## Setting up water

1. Scale a GameObject over the water, add `SolverVolume`, shape Box. Box has a
   flat top and therefore a waterline; Ellipsoid has none.
2. Add a `SolverMediumProfile` to `effects`.
3. `viscosity` around 1 to start — bodies visibly slow.
4. `density`: **the useful value is in the hundreds, and water is not 1000.**
   Units follow the profile mass and the global particle radius. The runner logs
   the exact neutral value per profile the first time a medium touches it. Read
   the console; do not guess. Above neutral floats, below sinks.
5. `flow` is metres per second the water itself moves. Bodies converge on it and
   stop, so it is authored directly rather than as a force.
6. `flowIsLocal` reads `flow` in the volume's own axes, so aiming the volume aims
   the flow. Leave it off for an ocean current, which is a property of the world;
   turn it on for anything aimed, or rotating the object will not change where it
   pushes. A profile is a shared asset, so a world-space flow is the same vector
   in every volume referencing it.

Leave `SolverManager.damping` alone. It is the solver's energy bleed, not a
stand-in for viscosity.

## A jet is a medium

A hose, a vent, a current, a downdraught: box `SolverVolume`, one
`SolverMediumProfile`, `density = 0` so it is pure push and no buoyancy.

1. Scale the box long and thin, **+Z along the spray**, and push the object
   forward by half its length — `Center` is the Transform position, so otherwise
   half the jet is behind the nozzle.
2. `flow = (0, 0, speed)` with `flowIsLocal` on.
3. `viscosity` in the **tens**. A modifier writes velocity once per FixedUpdate
   from outside the substep loop, so one write survives about `1/substeps` of it.

A ParticleSystem alongside it draws the water and owns nothing else; they share a
Transform and no state. PS particles cannot push solver particles and should not
try — a droplet is one tiny impulse, and what reads as *washed away* is the
sustained velocity field the medium already is.

Box costs three things: uniform push inside, a hard boundary, no spread. Two
existing settings to check before blaming the mechanism — `speedLimit` below the
jet speed decays the push, and any medium sets `submerged`, so a fish struck in
**air** starts swimming.

## Setting up locomotion

1. Add `SolverMotionTarget` on its **own** GameObject — not on the volume
   (moving it would move the water) and not on the emitter (spawning and
   destination are different things).
2. `mode` Point converges a group; Direction sends it in parallel.
3. `radius` separates groups without ids: a body follows the nearest target that
   reaches it, and 0 reaches everything.
4. Add `SolverLocomotionProfile` to the particle profile's modifier list.

It only acts on bodies inside a medium, because propulsion needs something to
push against; out of water a body goes limp with no rule saying so. If nothing
moves, read the console — it says when no medium exists.

Not obvious from the fields:

- `duration >= 1 / frequency` is continuous locomotion. There is no mode switch.
- The glide between pushes is the medium's viscosity, not a third state.
- `headingSpread` is re-rolled per push, so bodies wander instead of forming an
  arrow, and a few breach at a time rather than the whole group leaving together.
- Jumping is not a behaviour. It is a push whose heading points up, followed by a
  ballistic arc once the body leaves the medium: height is `v² / 2g`, so 3 m/s
  gives about half a metre. Angle the target rather than pointing it straight up,
  or the arc is vertical instead of a hill.

## Mesh conventions

- **The mesh's positive Forward Axis end is the head**, matching the topology's
  first particle. A mesh authored the other way swims backwards; set
  `flipForward` on the render profile. Nothing in the geometry says which end is
  the nose, which is why this is one of the few facts a field has to carry.
- Rigid profiles are drawn with the assigned Material **directly**, so any URP or
  HDRP material works. Leave `mesh` empty on a rigid profile and it draws the
  convex hull of its own particles.
- Articulated profiles need a mesh and use the package shader.

## Colliders

Unity's own `BoxCollider` and `Rigidbody` are invisible to the solver — separate
worlds, no bridge. Use the vendored `SolverBoxCollider`, `SolverSphereCollider`
or `SolverCapsuleCollider`. Geometry comes from the Transform, and a box is a
solid rather than a container, so a holding tank is built from thin walls.

## Anchoring particles to Transforms

`PhysicsParticleAnchor` holds chosen particles of a body at scene Transforms.
**One Transform binds many particles**, each keeping its own captured offset, so
anchoring an edge of cloth to a hand preserves that edge's shape.

It is backend neutral and resolves nothing itself. Pair it with the glue for the
body you have — both vendored generators need one, because they are read-only and
cannot implement the interface:

| Body | Glue component | Selector |
|---|---|---|
| `ClothGenerator` | `SolverClothParticles` | `(x, y)` |
| `RopeGenerator` | `SolverRopeParticles` | `(segment, 0)` |

- Offsets are **captured at author time** from the body's rest layout, not at
  runtime — in edit mode the generators have not spawned any particles, so the
  glue reproduces their spawn maths instead of reading a buffer. Move the body
  afterwards and press **Recapture Offsets**, or the offsets point at the old
  layout.
- **Turn off `RopeGenerator.fixStart` / `fixEnd` for any end an anchor holds.**
  They pin the end particle by setting inverse mass to zero. Anchoring still
  works, but two mechanisms then govern one particle and behaviour becomes
  impossible to attribute.
- Anchoring sets `invMass = 0` and **never restores it**. An anchor is permanent
  by intent; use `ClothGrabber` for grab-and-release, which does restore it.
- Runs at `-100`, before the solver, because an anchor states where a particle
  *is*. Do not move it after the solver like a modifier.

## Settings that silently ruin things

- `particleRadius` larger than a body's smallest dimension makes the collision
  shape much bigger than the visual, so nothing stacks and nothing looks right.
  Check it before blaming a feature.
- `collideWithSameProfile` false gives every instance one shared phase, and
  particles sharing a non-zero phase never collide. Fragments then pass through
  each other while still hitting fish, which does not read as a phase problem.
- Global `damping` raised to tame launches makes ordinary motion sluggish
  everywhere. Use `speedLimit`, which does nothing below its threshold.
- A spawn volume too small for its instance count overlaps bodies at t=0, and
  `maxDepenetrationSpeed` then throws them apart on the first frame. Divide the
  volume by the count and compare the spacing against the body size before
  blaming the launch on anything else.
- **Self-collision on a chain or lattice body makes its own neighbours push
  each other apart.** `enableSelfCollision` sets the body's phase to
  `PhaseNone`, which is `0`, and the contact kernel only skips a pair when both
  share the same **non-zero** phase — so nothing is excluded, adjacent particles
  included. Contact then demands `2 * particleRadius` between two particles a
  distance constraint is holding at `spacing`. Whenever
  `spacing < 2 * particleRadius` the two fight every step and the body buckles
  sideways to make room: a rope goes visibly crinkled, cloth ripples. The phase
  system is per body and all-or-nothing, so fine spacing and self-collision are
  mutually exclusive without an adjacency exclusion in the kernel. Check
  `spacing` against the **global** `particleRadius` before enabling it.
- **XPBD `compliance` is scaled by the substep dt, not the frame dt**, so its
  useful values are far smaller than they look. `alphaTilde = compliance / dt^2`
  with `dt = fixedDeltaTime / substeps` — at the default 30 substeps that is
  `0.02 / 30 = 6.7e-4`, so compliance is multiplied by about **2.2 million**
  before it reaches the solve. The correction applied is
  `wSum / (wSum + alphaTilde)`, and `wSum` is 2 for two free unit-mass
  particles. So compliance `1e-6` is roughly half stiffness, `1e-5` is soft,
  and anything from `1e-4` up applies well under 1% and is indistinguishable
  from having added no constraint at all. A "reasonable-looking" 0.001 does
  nothing, silently. Note it also moves with mass: heavier particles mean a
  smaller `wSum`, so the same compliance reads as softer.
- Low `frictionKinetic` is right for ice and wrong for a pile that holds its
  shape. Friction itself is correct and substep-independent — the per-substep
  limit `mu * penetration` works out to `a = mu * g` — but it turns sliding into
  rolling, and **a body made of spheres has no rolling resistance at all**, so a
  pile keeps creeping.

## When something does nothing

In this order:

1. **Read the console.** Most silent failures now name themselves: missing
   medium, missing material, missing shader, missing mesh, and the neutral
   density value.
2. **Check the value's scale.** `density` needing hundreds is the standing case.
3. **Check the channel.** Velocity aimed at a body in contact cannot work, and
   that is not a bug.
4. **Check the topology gate.** Several kernels apply only to chain topologies,
   so a feature that looks dead may simply not run on the topology in use.
