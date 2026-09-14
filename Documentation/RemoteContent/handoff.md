# Remote content learning plan handoff

## Scope and state

- Canonical design: [plan.md](plan.md). User requested a named learning and implementation plan, not implementation or deployment.
- Plan authored 2026-09-09. No runtime code, asset repository, R2 bucket, workflow or package was created. Runtime outcomes in the acceptance list remain unverified.
- Root handoff and reports already contain other uncommitted work; they were preserved. This directory owns this task's handoff.
- Updated discussion scope: Git LFS first; character A avatar prepared at home, B avatar requested by home completion action, C excluded. Component/service/presentation responsibilities and optional RemoteContent package are proposals, not implemented contracts.
- Independent review of this revision found no significant inconsistency or missing requested coverage. Open-decision links and whitespace checked. Runtime and LFS acceptance remain unexecuted.

## Next steps

- Settle [待討論決策](plan.md#待討論決策) before component/package implementation.
- Start [學習與實作順序](plan.md#學習與實作順序), stage 1: resolve the validation project and asset repository, then prove LFS retrieval with preserved GUIDs. R2 runtime verification is stage 2.
- Use [驗收清單](plan.md#驗收清單) to capture network/build-layout evidence as each prototype stage runs.

## Open decisions

- [待討論決策](plan.md#待討論決策): avatar overlay meaning, nonblocking menu entry, optional package location, progress integration.

## Ruled-out directions

- Separate local Asset Database runtime and remote production implementations: user explicitly wants one remote path.
- Git LFS URLs as runtime Addressables hosting: source assets require Unity content builds; R2 serves build outputs.
- Automatically preloading both permitted avatars before menu entry was replaced by initial A first and event-triggered B, to expose visible asynchronous work in the first validation case.
- Large models/scenes are deferred extensions; requiring them for the first proof would obscure the user's avatar-first learning objective.
