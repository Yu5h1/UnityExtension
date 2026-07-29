# Yu5h1 Unified Solver Extension Handoff

## Scope

Target: `Assets/Yu5h1Lib/unified-solver`

The original `Assets/unified-solver` repository is a read-only dependency. Its git worktree was verified clean after implementation.

## Implemented

- `SolverParticleSpawnRequest`, `SolverParticleInstance`, topology/render enums.
- ScriptableObject profiles for particle topology, render data, oscillation and surface impulse.
- `SolverParticleEmitter` with append-only dynamic queue, safe pre-solver flush, capacity checks, topology builders for Single/Chain3/GuideChain4/DualRail6/RigidCluster4/ArticulatedCluster12, instance mapping buffer, scale and angular velocity support.
- `SolverMeshRenderer` with rigid and articulated procedural GPU rendering.
- `SolverParticleModifierRunner` plus batched `ApplyOscillation` and `ApplySurfaceImpulse` kernels.
- Oscillation now projects a profile-controlled C-bend after the passive Solver constraints: `bendRatio` is a geometric middle-control offset relative to body length, `frequency` controls only the phase interval, and position/velocity corrections remain momentum-balanced across head/middle/tail groups.
- `ParticleSystemSolverBridge` with Trigger Enter batch conversion, local/world/custom simulation-space conversion, size/color/rotation/linear/angular velocity transfer, and accepted-only source particle removal.
- `SolverManagerAccess` compatibility bridge for the original solver's private rigid-body buffers, rigid-particle reference count, and `ClothGenerator` particle range. Rigid and cloth operations fail closed when their respective compatibility contract is unavailable.
- `ClothAnchor` and `ClothGrabber` resolve the original `ClothGenerator` range through the compatibility bridge; neither depends on the modified fork's `IsInitialized`, `ParticleOffset`, or `ParticleCount` properties.
- IL2CPP linker preservation for the reflected `SolverManager` and `ClothGenerator` fields.
- `Documentation/ParticleSystem x Unified Solver.md` defines ParticleSystem/Solver ownership boundaries, five cooperation modes, current component placement, phased validation, and long-term Soft Body goals without scheduling Soft Body implementation.
- README and architecture plan.

## Verification

- Runtime extension sources compile against the current original solver and Unity 6000.3.9f1 references with 0 warnings / 0 errors.
- Runtime compatibility tests compile with 0 warnings / 0 errors and cover field-contract resolution, rigid-particle reference count reads, pre-allocation buffer reads, and original `ClothGenerator` particle-range reads.
- No extension source directly references the removed `RigidBodyBuffer`, `RigidParticleIndexBuffer`, or `RigidParticleRefCount` properties.
- No extension source references the modified fork-only `ClothGenerator.IsInitialized`, `ParticleOffset`, or `ParticleCount` properties.
- Original `SolverManager.cs` SHA-256 remains `4E902F723AF3B6C6D2640683A517340F24D12651BC328EBE49C5C24A27992483`.
- Original `ClothGenerator.cs` SHA-256 remains `EF927603C0D7A9A9B7A118FA7C0EBC4420AC02B6EB178548615F8142E744566B`.
- The validation assembly was generated directly from the current Runtime sources.
- No generic runtime class/shader/compute name contains Fish or Ice.

## Pending Unity verification

The Unity editor instance was running without background AssetDatabase refresh. On returning focus/opening the project:

1. Wait for `.meta` generation and script reload.
2. Confirm both shaders and `SolverParticleModifiers.compute` import without errors.
3. Create Render/Profile assets and run Chain3 then RigidCluster4 smoke tests.
4. Configure a ParticleSystem Trigger and verify accepted-only conversion.
5. Run `SolverManagerAccessTests` in the Unity Test Runner.
6. Make an IL2CPP Development Build to confirm reflected field metadata is preserved.
7. Tune and replay the BonghuoVR DualRail6 fish profile (`bendCompliance: 0.05`, `acceleration: 300`, `frequency: 6`, `bendRatio: 0.45`, low randomness) in Play Mode; true S-curves remain outside the current three-longitudinal-control topology.

## Known limitations

- Global particle radius remains owned by the original solver.
- Instances are append-only; no free list/recycling yet.
- Cross-emitter capacity reservation is not globally atomic at the final capacity edge.
- Modifier dispatch is batched per emitter, not yet globally across all emitters sharing a modifier type.
- The non-invasive compatibility bridge depends on the names and types of three private `SolverManager` fields and the private `ClothGenerator._particleOffset` field.
- The original solver always performs synchronous rigid-body readback; the extension cannot disable it without changing the original source.
