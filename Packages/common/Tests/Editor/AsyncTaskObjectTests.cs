using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// <see cref="AsyncTaskObject"/>, <see cref="AsyncProgressObject"/>, <see cref="AsyncLoadObject{T}"/> and
    /// <see cref="ResultJudge"/> awaited straight from code: event order with a synchronous and an asynchronous judge,
    /// a run without a judge, cancellation, progress, and loading an object or nothing. Work and judges wait on
    /// completion sources the test resolves, so no test depends on frames advancing.
    /// </summary>
    public class AsyncTaskObjectTests
    {
        private readonly List<Object> created = new List<Object>();
        private readonly List<string> log = new List<string>();

        private class GatedTask : AsyncTaskObject
        {
            public AwaitableCompletionSource<string> gate = new AwaitableCompletionSource<string>();

            protected override async Awaitable<string> Processing(CancellationToken token)
            {
                using (token.Register(() => gate.TrySetCanceled()))
                    return await gate.Awaitable;
            }
        }

        private class SteppedProgress : AsyncProgressObject
        {
            public float[] steps;
            public AwaitableCompletionSource<string> gate = new AwaitableCompletionSource<string>();

            protected override async Awaitable<string> Processing(CancellationToken token)
            {
                foreach (var step in steps)
                    ReportProgress(step);
                return await gate.Awaitable;
            }
        }

        private class TextureLoad : AsyncLoadObject<Texture2D>
        {
            public string address = "characters/test";
            public AwaitableCompletionSource<Texture2D> gate = new AwaitableCompletionSource<Texture2D>();

            public override string key => address;

            protected override async Awaitable<Texture2D> Load(CancellationToken token)
            {
                ReportProgress(0.5f);
                var texture = await gate.Awaitable;
                ReportProgress(1f);
                return texture;
            }
        }

        private class OkJudge : ResultJudge
        {
            public List<string> log;

            protected override JudgeResult Judge(string input)
            {
                log.Add($"Judge:{input}");
                return new JudgeResult(input == "ok", $"judged {input}");
            }
        }

        private class SecondStageJudge : ResultJudge
        {
            public List<string> log;
            public AwaitableCompletionSource<bool> verdict = new AwaitableCompletionSource<bool>();

            public override async Awaitable<JudgeResult> JudgeAsync(string input, CancellationToken token = default)
            {
                log.Add($"Judge:{input}");
                var succeeded = await verdict.Awaitable;
                log.Add("Verified");
                return new JudgeResult(succeeded, $"verified {input}");
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
                if (obj != null)
                    Object.DestroyImmediate(obj);
            created.Clear();
            log.Clear();
        }

        private T Create<T>() where T : ScriptableObject
        {
            var obj = ScriptableObject.CreateInstance<T>();
            obj.name = typeof(T).Name;
            created.Add(obj);
            return obj;
        }

        private T Track<T>(T task) where T : AsyncTaskObject
        {
            task.onBegin.AddListener(s => log.Add($"Begin:{s}"));
            task.onEnd.AddListener(s => log.Add($"End:{s}"));
            return task;
        }

        private T Track<T>(T judge, List<string> into) where T : ResultJudge
        {
            judge.onTrue.AddListener(s => into.Add($"True:{s}"));
            judge.onFalse.AddListener(s => into.Add($"False:{s}"));
            judge.onCompleted.AddListener(b => into.Add($"Completed:{b}"));
            return judge;
        }

        private OkJudge CreateOkJudge()
        {
            var judge = Track(Create<OkJudge>(), log);
            judge.log = log;
            return judge;
        }

        [Test]
        public async Task SyncJudge_Success_RaisesBeginEndJudgeTrueCompleted()
        {
            var task = Track(Create<GatedTask>());
            task.judge = CreateOkJudge();
            task.gate.SetResult("ok");

            var succeeded = await task.ExecuteAsync();

            Assert.IsTrue(succeeded);
            Assert.IsTrue(task.Outcome.Succeeded);
            Assert.AreEqual("judged ok", task.Outcome.Content);
            CollectionAssert.AreEqual(new[]
            {
                "Begin:GatedTask", "End:ok", "Judge:ok", "True:judged ok", "Completed:True"
            }, log);
        }

        [Test]
        public async Task SyncJudge_Failure_RaisesFalseThenCompletedFalse()
        {
            var task = Track(Create<GatedTask>());
            task.judge = CreateOkJudge();
            task.gate.SetResult("bad");

            var succeeded = await task.ExecuteAsync();

            Assert.IsFalse(succeeded);
            Assert.IsFalse(task.Outcome.Succeeded);
            CollectionAssert.AreEqual(new[]
            {
                "Begin:GatedTask", "End:bad", "Judge:bad", "False:judged bad", "Completed:False"
            }, log);
        }

        [Test]
        public async Task AsyncJudge_IsAwaitedBeforeResultEvents()
        {
            var task = Track(Create<GatedTask>());
            var judge = Track(Create<SecondStageJudge>(), log);
            judge.log = log;
            task.judge = judge;
            task.gate.SetResult("payload");
            judge.verdict.SetResult(false);

            var succeeded = await task.ExecuteAsync();

            Assert.IsFalse(succeeded);
            CollectionAssert.AreEqual(new[]
            {
                "Begin:GatedTask", "End:payload", "Judge:payload", "Verified", "False:verified payload", "Completed:False"
            }, log);
        }

        [Test]
        public async Task NoJudge_RaisesOnlyBeginAndEnd()
        {
            var task = Track(Create<GatedTask>());
            task.gate.SetResult("content");

            var succeeded = await task.ExecuteAsync();

            Assert.IsTrue(succeeded);
            CollectionAssert.AreEqual(new[] { "Begin:GatedTask", "End:content" }, log);
        }

        [Test]
        public async Task Cancel_RaisesEndAndCompletedFalse_WithoutJudging()
        {
            var task = Track(Create<GatedTask>());
            task.judge = CreateOkJudge();
            using (var cts = new CancellationTokenSource())
            {
                var run = task.ExecuteAsync(cts.Token);
                Assert.IsTrue(task.IsRunning);
                cts.Cancel();

                var succeeded = await run;

                Assert.IsFalse(succeeded);
            }
            Assert.IsTrue(task.IsCanceled);
            Assert.IsFalse(task.IsRunning);
            Assert.IsFalse(task.Outcome.Succeeded);
            CollectionAssert.AreEqual(new[] { "Begin:GatedTask", "End:", "Completed:False" }, log);
        }

        [Test]
        public async Task CancelDuringAsyncJudge_RaisesCompletedFalseOnly()
        {
            var task = Track(Create<GatedTask>());
            var judge = Track(Create<SecondStageJudge>(), log);
            judge.log = log;
            task.judge = judge;
            task.gate.SetResult("payload");
            using (var cts = new CancellationTokenSource())
            {
                var run = task.ExecuteAsync(cts.Token);
                cts.Cancel();
                judge.verdict.SetCanceled();

                Assert.IsFalse(await run);
            }
            Assert.IsTrue(task.IsCanceled);
            CollectionAssert.AreEqual(new[] { "Begin:GatedTask", "End:payload", "Judge:payload", "Completed:False" }, log);
        }

        [Test]
        public async Task Progress_RisesWithinRange_BetweenBeginAndEnd()
        {
            var task = Track(Create<SteppedProgress>());
            task.steps = new[] { 0f, 0.25f, 0.2f, 0.75f, 1.5f };
            task.onProgress.AddListener(p => log.Add($"Progress:{p}"));
            task.gate.SetResult("done");

            await task.ExecuteAsync();

            CollectionAssert.AreEqual(new[]
            {
                "Begin:SteppedProgress", "Progress:0", "Progress:0.25", "Progress:0.25", "Progress:0.75", "Progress:1", "End:done"
            }, log);
            Assert.AreEqual(1f, task.Progress);
        }

        [Test]
        public async Task Load_Found_RaisesLoadedAndSucceeds()
        {
            var task = Track(Create<TextureLoad>());
            var texture = new Texture2D(1, 1);
            created.Add(texture);
            var loaded = new List<Texture2D>();
            var progress = new List<float>();
            task.onLoaded.AddListener(loaded.Add);
            task.onProgress.AddListener(progress.Add);
            task.gate.SetResult(texture);

            var succeeded = await task.ExecuteAsync();

            Assert.IsTrue(succeeded);
            Assert.AreSame(texture, task.Result);
            CollectionAssert.AreEqual(new[] { texture }, loaded);
            CollectionAssert.AreEqual(new[] { 0.5f, 1f }, progress);
            CollectionAssert.AreEqual(new[] { "Begin:characters/test", "End:characters/test" }, log);
        }

        [Test]
        public async Task Load_Missing_FailsWithoutLoaded()
        {
            var task = Track(Create<TextureLoad>());
            var loaded = new List<Texture2D>();
            task.onLoaded.AddListener(loaded.Add);
            task.gate.SetResult(null);

            var succeeded = await task.ExecuteAsync();

            Assert.IsFalse(succeeded);
            Assert.IsNull(task.Result);
            Assert.IsEmpty(loaded);
            CollectionAssert.AreEqual(new[] { "Begin:characters/test", "End:characters/test" }, log);
        }

        [Test]
        public async Task Load_MissingWithJudge_ReportsFalseWithoutCallingJudge()
        {
            var task = Track(Create<TextureLoad>());
            task.judge = CreateOkJudge();
            task.gate.SetResult(null);

            Assert.IsFalse(await task.ExecuteAsync());
            CollectionAssert.AreEqual(new[]
            {
                "Begin:characters/test", "End:characters/test", "False:characters/test", "Completed:False"
            }, log);
        }

        [Test]
        public async Task Load_FoundThenRunMissing_ClearsResult()
        {
            var task = Create<TextureLoad>();
            var texture = new Texture2D(1, 1);
            created.Add(texture);
            task.gate.SetResult(texture);
            Assert.IsTrue(await task.ExecuteAsync());

            task.gate = new AwaitableCompletionSource<Texture2D>();
            task.gate.SetResult(null);
            Assert.IsFalse(await task.ExecuteAsync());
            Assert.IsNull(task.Result);
        }
    }
}
