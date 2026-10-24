# Async task objects

How to use the `Awaitable` job objects in `com.yu5h1.common` (`Packages/common/Runtime/Task/`, namespace
`Yu5h1Lib`, assembly `Yu5h1Lib.Common`). Design and reasons:
[非同步工作](../../Documentation/非同步工作.md) (Task ID `async-task`). Tests that show every rule below:
`Packages/common/Tests/Editor/AsyncTaskObjectTests.cs`.

## Types

| Type | Is | You override |
|---|---|---|
| `AsyncTaskObject` | Abstract inline SO: Begin<string>, End<string>, optional `judge` field | `Processing(CancellationToken)` → `Awaitable<string>` (the End content); optionally `beginContent` (default: the object's name) |
| `AsyncProgressObject : AsyncTaskObject` | Adds Progress<float> | Call `ReportProgress(float)` inside `Processing` |
| `AsyncLoadObject<T> : AsyncProgressObject` (`T : UnityEngine.Object`) | Adds Loaded<T> and `Result` | `key` (address or path; Begin and End carry it) and `Load(CancellationToken)` → `Awaitable<T>` (null when nothing was found) |
| `ResultJudge` | Abstract inline SO that decides success from the End content; owns True<string>, False<string>, Completed<bool> | `Judge(string)` for a synchronous rule, or `JudgeAsync(string, CancellationToken)` for one that awaits |
| `JudgeResult` | `Succeeded` + `Content` (what True/False receive) | — |

House conventions apply: no `[CreateAssetMenu]`; the task's `judge` field is already `[Inline]`, and a field of
yours that references a task or judge needs `[Inline]` too.

## Event order

```text
Begin<string> → [Progress<float>] → End<string> → judge (awaited) → [Loaded<T>] → True<string>／False<string> → Completed<bool>
```

- End carries the full content `Processing` returned; it is the judge's input.
- The task awaits the judge, stores the result in `Outcome`, then calls `judge.Report`, which raises True or False
  with `JudgeResult.Content` and then Completed with the success flag.
- No judge: only Begin and End (and Progress/Loaded); the run counts as a success, except `AsyncLoadObject<T>`, which
  succeeds only when it loaded an object.
- Progress values are clamped to 0..1 and never go down within a run; `Progress` holds the last one.
- `AsyncLoadObject<T>`: a missing object fails without calling the judge (the judge still raises False(key) and
  Completed(false)); a loaded object goes to the judge if there is one, which may reject it. On success `Result` is
  the object and Loaded fires; otherwise `Result` is null and Loaded does not fire.

## Cancellation and failure

- Cancel through the token passed to `ExecuteAsync`. A canceled run raises End("") and the judge's Completed(false),
  never calls the judge and never raises True or False; `ExecuteAsync` returns false without throwing and
  `IsCanceled` is true. Cancellation while an async judge is awaiting behaves the same, after End already fired.
- Any other exception from `Processing` or the judge raises End (if not yet raised) and Completed(false), then
  propagates out of `ExecuteAsync`.
- End unlocks the UI, Completed closes the loading screen: both fire on every path, so the screen always recovers.

## Writing a judge

```csharp
public class NotEmptyJudge : ResultJudge
{
    protected override JudgeResult Judge(string input) => new JudgeResult(!string.IsNullOrEmpty(input), input);
}

public class ServerCheckJudge : ResultJudge
{
    public override async Awaitable<JudgeResult> JudgeAsync(string input, CancellationToken token = default)
    {
        await Awaitable.WaitForSecondsAsync(0.1f, token);
        return new JudgeResult(input.Contains("\"ok\""), input);
    }
}
```

A judge that overrides neither method throws `NotImplementedException` when judged.

## Calling from code

`Awaitable` never appears in a serialized field or a UnityEvent signature; code awaits the task directly:

```csharp
public class CharacterLoad : AsyncLoadObject<GameObject>
{
    [SerializeField] private string _address;
    public override string key => _address;

    protected override async Awaitable<GameObject> Load(CancellationToken token)
    {
        ReportProgress(0f);
        await Awaitable.NextFrameAsync(token);
        ReportProgress(1f);
        return Resources.Load<GameObject>(_address);
    }
}

// Caller (e.g. a MonoBehaviour holding [SerializeField, Inline] CharacterLoad _load):
if (await _load.ExecuteAsync(destroyCancellationToken))
    Instantiate(_load.Result);
else if (!_load.IsCanceled)
    Debug.LogWarning($"{_load.key}: {_load.Outcome.Content}");
```

Awaitable instances are pooled: await each `ExecuteAsync` result once. One run per object at a time; a second
`ExecuteAsync` before the first finishes is not handled (replacing a run as a cancellation is left to the ChatRequest
migration).

Tests: EditMode `async Task` tests (Test Framework 1.4) can await `ExecuteAsync` directly; hold the work on an
`AwaitableCompletionSource<T>` the test resolves so nothing depends on frames advancing. To expect an exception from
an `Awaitable` that is still pending, use `try { await … } catch` in the test: NUnit's `Assert.CatchAsync`/`ThrowsAsync`
block the main thread on the task, and the Awaitable needs that thread to finish, so the test hangs (Yu5h1LibTest
PlayMode 2026-10-24). They work only when the call throws before its first await.
