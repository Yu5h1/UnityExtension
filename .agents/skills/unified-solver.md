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

## The scene that is known to work

`Packages/Unified-Solver/Runtime/Test/test solver mesh.unity`, opened from
BonghuoVR. Fish schooling, breaching, a net, and catching them in it are all
verified there. **Read it before deriving a setup from first principles** — a
working assembly answers faster than this file does, and its numbers are real
rather than plausible.

```
unified-solver        SolverManager        substeps 30, damping 2, particleRadius 0.05
                                           frictionStatic 0.02, frictionKinetic 0.005
SolverVolume/
  Sea                 SolverVolume + PrimitiveShape (Box)   -> Medium  density 472
  Bound               SolverVolume + PrimitiveShape         -> Bounds  actOutside, fade 0.5/0.5
  FishGenerator       SolverParticleEmitter  256 of 512, spawnVolume 5x2x5
    SolverMotionTarget                       mode Direction, radius 0
splash water          SolverVolume + ParticleSystemAddon    -> Medium  density 1, flow (0,0,2000) local
box medium            SolverVolume + PrimitiveShape         -> Medium  (the same profile object as Sea)
box/Cube x5           SolverBoxCollider        thin walls making a tank
cloth-generator       ClothGenerator 100x100   + SolverClothParticles + PhysicsParticleAnchor
hand/grab             ClothGrabber             grabRadius 0.89
Ice-generator         SolverParticleEmitter    100 of 2048, ice profile
pip                   RopeGenerator 59 seg     + SolverRopeParticles + PhysicsParticleAnchor
```

Two things in that list are easy to misread. `Sea` is **disabled** — the active
water is `box medium`, which references the *same* medium profile object — an
unsaved ScriptableObject living in the scene, not an asset file, so searching the
Project window for it finds nothing. The two are alternative placements of one
body of water, not two bodies. And `splash water`
takes its geometry from a **ParticleSystem**, so the cone the artist sees in the
Shape module *is* the jet region.

The verified profile values, for comparison rather than for copying:

| | Fish | Ice |
|---|---|---|
| `topology` | `GuideChain4` | `RigidCluster6` + `SolverIceFragmentShapeSource` |
| `renderProfile.mesh` | fish FBX, `forwardAxis` Y, `flipForward` | empty — draws its own hull |
| `bendCompliance` | `0.05` (limp) | `0.0005` (stiff) |
| `rollDamping` | `1` | `0.25` |
| `sleepSpeed` | `0.05` | `1` — ice settles hard, fish must not |
| `modifiers` | Oscillation + Locomotion | Oscillation |

Fish locomotion as tuned: `speed 5`, `mediumThrust 1`, `frequency 1`,
`duration 0.3`, `headingSpread 25`, `turnRate 180`, `uprightRate 120`.
Oscillation is tuned far lower than its defaults — `vitality 0.065`,
`muscleTension 0`, `stiffness 0` — because locomotion now supplies the travel
and oscillation only has to supply the wriggle.

## Volumes carry a list of effects

`SolverVolume` is geometry only, plus an `effects` list. One box can be water
inside **and** recycle anything leaving, without two components duplicating the
same geometry and inside test.

**The geometry is not this component's Transform.** It comes from a separate
component implementing `IShapeProvider`, and from nowhere else — there is no
fallback, because a fallback is what would make a mis-wired volume move
sometimes and do nothing other times with nothing on screen saying which. A
volume with no provider simply does not run.

| Provider | Gives | Use for |
|---|---|---|
| `PrimitiveShape` | Box, Sphere, Cylinder, Cone from the Transform | most regions |
| `ParticleSystemAddon` | the PS Shape module's own volume | a region the VFX already draws |

`Reset` adds a `PrimitiveShape` for you, so a volume added through the Inspector
is wired already. Only **Box** has a flat top, and a flat top is what a waterline
is; a sphere's surface height varies with horizontal position, so a body floating
up in one settles at a different level depending on where it happens to be.

`Shape` may also be a GameObject rather than the component — dragging a provider
out of the hierarchy hands over the GameObject, and the field resolves it.

Every effect carries `actOutside`, because that motivating case needs both sides
of one surface at once.

| Effect | Granularity | Does |
|---|---|---|
| `SolverMediumProfile` | Particle | density, flow |
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
Water           <- SolverVolume + PrimitiveShape (Box) -> [SolverMediumProfile]
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

1. Scale a GameObject over the water and add `SolverVolume`. It brings a
   `PrimitiveShape`; leave that on Box. Box has a flat top and therefore a
   waterline; the rounded kinds have none.
2. Add a `SolverMediumProfile` to `effects`.
3. `density`: **the useful value is in the hundreds, and water is not 1000.**
   Units follow the profile mass and the global particle radius. The runner logs
   the exact neutral value per profile the first time a medium touches it. Read
   the console; do not guess. Above neutral floats, below sinks. **The same
   number is the coupling baseline** — see below.

   Author the **direction**, not the real figure. This is not kg/m3 and never
   will be, so the question to answer is "denser or thinner than this body, and
   by roughly how much" — still water dense, spray thin, air far thinner. A
   setting that reads correctly against the neutral value behaves correctly;
   chasing 1000 for water buys nothing and hides what the number means.
4. `flow` is metres per second the water itself moves. Bodies converge on it and
   stop, so it is authored directly rather than as a force.
5. `flowIsLocal` reads `flow` in the volume's own axes, so aiming the volume aims
   the flow. Leave it off for an ocean current, which is a property of the world;
   turn it on for anything aimed, or rotating the object will not change where it
   pushes. A profile is a shared asset, so a world-space flow is the same vector
   in every volume referencing it.

**How hard the medium drags is not on the medium.** A medium carries only
`density` and `flow`; the coefficient is `SolverParticleProfile.dragCoefficient`
on the **body**, because drag is `½ρC_dAv²` and only `ρ` is the fluid's. It
multiplies the medium-to-body density ratio, so a medium at the neutral density
gives a ratio of 1 and a coefficient of 1 is the neutral starting point.

The same coupling is what locomotion pushes off, scaled by
`SolverLocomotionProfile.mediumThrust`. Being carried and being able to swim are
one thing seen from two sides, so they can never contradict each other.

Leave `SolverManager.damping` alone. It is the solver's energy bleed, not a
per-medium control.

## A jet is a medium

A hose, a vent, a current, a downdraught: box `SolverVolume`, one
`SolverMediumProfile`.

1. Scale the box long and thin, **+Z along the spray**, and push the object
   forward by half its length — `Center` is the Transform position, so otherwise
   half the jet is behind the nozzle.
2. `flow = (0, 0, speed)` with `flowIsLocal` on.
3. `density` near neutral and a large `flow`. A modifier writes velocity once per
   FixedUpdate from outside the substep loop, so one write survives about
   `1/substeps` of it — a jet needs a big disagreement to win.

**A spray is thin and fast, and authoring it that way is enough.** Water leaving
a pipe is dispersed — it is genuinely less dense than the still body it came
from — and it is genuinely moving fast. So `density` well below neutral with a
large `flow` is not a trick; it is the description.

The behaviour follows on its own:

```
carried away  ∝  coupling × (flow − velocity)     ← the speed makes up the loss
thrust        ∝  coupling × mediumThrust          ← nothing makes it up
```

Both weaken with density, but only the first has a term to compensate with. So a
body is swept along and cannot swim against it — which is what a real spray does,
and why nothing swims up a leaf blower either.

Two costs to expect: pickup is gradual, because a low coupling approaches the
flow speed slowly, so a short region may release the body before it is up to
speed; and buoyancy falls with density, so bodies sink while inside.

Do **not** reach for `density = 0`. Nothing real is weightless, and the ratio
goes to zero, so the jet stops dragging as well.

**Let the ParticleSystem that draws the spray own the geometry.** Put
`SolverVolume` on it and point `_shapeProvider` at its `ParticleSystemAddon`:
the cone in the Shape module then *is* the medium, so the region can never drift
out of step with what the artist sees, and aiming the VFX aims the push. This is
how `splash water` is built in the reference scene. Its Cone opens along +Z, the
same axis as `ShapeKind.Cone`, so nothing has to be reconciled — but a Circle,
Edge, Rectangle, Sprite or Mesh shape encloses no volume and `IsUsable` reports
false rather than inventing a depth.

The PS particles themselves still push nothing and should not try — a droplet is
one tiny impulse, and what reads as *washed away* is the sustained velocity field
the medium already is.

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
- The glide between pushes is the medium coupling bleeding the speed off, not a third state.
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

### A net, and catching things in it

The whole catching mechanism is: a cloth soft enough to bag, its corners held by
Transforms, and those Transforms moved by anything at all. Nothing in the package
knows the word "net".

1. `ClothGenerator` for the mesh. Verified: `resolutionX/Y 100`, `spacing 0.1`,
   `compliance 1e-7`, `constraintDamping 99999`, every `fix*Edge` **off**,
   `enableSelfCollision` **off**, `fixedParticlesFollowTransform` on.
2. Add `SolverClothParticles` beside it, then `PhysicsParticleAnchor`.
3. In the anchor's Scene view, click the grid to pick the corner particles and
   drop each on its own Transform. Those Transforms are the rim: parent them to
   hands, a frame, a winch, an animation — the anchor neither knows nor cares.
4. Move the rim. A bag forms, fish inside cannot leave through it, and lifting it
   out of the medium is the catch — they go limp on the way up because locomotion
   has nothing left to push against, with no rule anywhere saying so.

Why those cloth numbers and not softer-looking ones: `compliance 1e-7` is near
rigid *after* the substep scaling (see the compliance entry below), and
`constraintDamping 99999` is only usable *because* the compliance is that small —
`gamma = compliance * beta / subDt`, so the two have to be tuned as a pair and a
damping that large against an ordinary compliance does nothing. Leave the fix
edges off: pinning sets inverse mass to zero and would fight the anchors for the
same particles.

`ClothGrabber` is the other half — grab and **release**, with inverse mass
restored. Use it for a hand; use `PhysicsParticleAnchor` for a rim that is held
permanently.

## Driving it from script

Most of this system is authored, not called. The runtime surface is small, and
knowing it is small saves hunting for an API that was never going to exist.

| To do this | Call / change |
|---|---|
| spawn one body | `SolverParticleEmitter.SpawnOne()` |
| spawn a specific body | `TryEnqueueSpawn(request)`, then `FlushQueued()` |
| spawn many | `EnqueueBatch(...)` — reserves capacity once |
| hold back the initial pile | `playOnAwake = false` |
| move a shoal | move the `SolverMotionTarget` Transform |
| split shoals | give each target a `radius`; a body follows the nearest that reaches it |
| turn water off | disable the `SolverVolume`, or set an effect's `enabled` |
| aim a jet | rotate the volume, with `flowIsLocal` on |
| grab cloth | `ClothGrabber.Grab()` / `Release()` |
| retune anything | edit the profile asset — it is live in play mode |

Two consequences of that table worth stating outright.

**`SolverMotionTarget` is just a Transform.** Timeline, a tween, a spline, a
NavMeshAgent, or feeding bait by hand all drive it the same way, and Unity already
animates Transforms better than anything written here would. There is no
scripting interface for "where the shoal goes" because there did not need to be.

**There is no group object.** A group is whichever bodies happen to be in range
of a target, so there is nothing to add or remove a body from, and no component
named after one. Asking which fish are in a shoal is asking the wrong question.

Capacity is the one place a script can quietly fail: `TryEnqueue` returns false
past `maxInstances`, and the solver's own `maxParticles` / `maxRigidBodies` on
`SolverManager` are separate ceilings that an emitter cannot raise.

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
