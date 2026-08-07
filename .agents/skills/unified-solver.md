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
