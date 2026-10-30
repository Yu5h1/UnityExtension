using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// <see cref="ContentSlot.Run(string, Func{IProgress{float}, Awaitable{bool}})"/> does a host's Report and Complete
    /// calls: progress becomes Report, the result Complete, an exception a failure, a cancellation nothing.
    /// </summary>
    public class ContentSlotRunTests
    {
        [Test]
        public async Task ProgressAndResultReachTheSlot()
        {
            var slot = new ContentSlot(new[] { "a" });
            var gate = new AwaitableCompletionSource<bool>();
            IProgress<float> reporter = null;
            slot.Select("a");
            var run = slot.Run("a", progress =>
            {
                reporter = progress;
                return gate.Awaitable;
            });

            reporter.Report(0.5f);
            Assert.AreEqual(ContentSlot.Indicator.Percent, slot.indicator);
            Assert.AreEqual(50, slot.percent);

            gate.SetResult(true);
            await run;
            Assert.AreEqual("a", slot.shown);
        }

        [Test]
        public async Task UnityEventProgressIsListenedOnlyDuringTheLoad()
        {
            var slot = new ContentSlot(new[] { "a" });
            var progress = new UnityEvent<float>();
            var gate = new AwaitableCompletionSource<bool>();
            slot.Select("a");
            var run = slot.Run("a", progress, () => gate.Awaitable);

            progress.Invoke(0.25f);
            Assert.AreEqual(25, slot.percent);
            gate.SetResult(true);
            await run;

            Assert.AreEqual("a", slot.shown);
            Assert.AreEqual(ContentSlot.Indicator.Hidden, slot.indicator);
        }

        [Test]
        public async Task ExceptionIsAFailure()
        {
            var slot = new ContentSlot(new[] { "a" });
            var gate = new AwaitableCompletionSource<bool>();
            slot.Select("a");
            var run = slot.Run("a", _ => gate.Awaitable);

            LogAssert.Expect(LogType.Exception, "InvalidOperationException: broken");
            gate.SetException(new InvalidOperationException("broken"));
            await run;

            Assert.AreEqual(ContentSlot.Fallback, slot.shown);
            Assert.IsTrue(slot.noticeVisible);
        }

        [Test]
        public async Task CancellationReportsNothing()
        {
            var slot = new ContentSlot(new[] { "a" });
            var gate = new AwaitableCompletionSource<bool>();
            slot.Select("a");
            var run = slot.Run("a", _ => gate.Awaitable);

            gate.SetCanceled();
            await run;

            Assert.AreEqual(ContentSlot.Placeholder, slot.shown);
            Assert.IsFalse(slot.noticeVisible);
        }
    }
}
