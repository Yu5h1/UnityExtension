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

## Recent Work

- 2026-07-27: Removed the `ParticleSystemRigidbody` implementation and its dedicated documentation. The Particle System C# Job approach remained CPU-bound and did not meet the required collision quality or performance for dense interactions.
- Decision: use `unified-solver` for large-scale collision, stacking, and container interactions. The retrospective is recorded in [DevelopmentLog.md](Documentation/DevelopmentLog.md).

