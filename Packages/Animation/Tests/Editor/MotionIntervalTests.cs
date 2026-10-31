using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.EditorExtension;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.Tests
{
    public class MotionIntervalTests
    {
        const string Withdraw =
            "{\"asset\":{\"version\":\"2.0\"},\"animations\":[{\"name\":\"withdraw\",\"samplers\":[],\"channels\":[]," +
            "\"extras\":{\"yu5h1\":{\"intervals\":{\"fps\":30,\"length\":15,\"duration\":0.5,\"items\":[" +
            "{\"name\":\"uncontrollable\",\"start\":0,\"end\":10,\"startTime\":0,\"endTime\":0.33333334,\"underControl\":false,\"cancellable\":false}," +
            "{\"name\":\"\\u53ef\\u53d6\\u6d88\",\"start\":10,\"end\":15,\"startTime\":0.33333334,\"endTime\":0.5,\"underControl\":true,\"cancellable\":true}]}}}}," +
            "{\"name\":\"plain\",\"samplers\":[],\"channels\":[]}]}";

        static byte[] Glb(string json)
        {
            var payload = Encoding.UTF8.GetBytes(json);
            var padded = (payload.Length + 3) / 4 * 4;
            var bytes = new byte[20 + padded];
            Array.Fill(bytes, (byte)0x20, 20, padded);
            BitConverter.GetBytes(0x46546c67u).CopyTo(bytes, 0);
            BitConverter.GetBytes(2u).CopyTo(bytes, 4);
            BitConverter.GetBytes((uint)bytes.Length).CopyTo(bytes, 8);
            BitConverter.GetBytes((uint)padded).CopyTo(bytes, 12);
            BitConverter.GetBytes(0x4e4f534au).CopyTo(bytes, 16);
            payload.CopyTo(bytes, 20);
            return bytes;
        }

        static MotionIntervals Made(GlbIntervalReader.Animation animation)
        {
            var intervals = ScriptableObject.CreateInstance<MotionIntervals>();
            intervals.Set(null, animation.frameRate, animation.length, animation.duration, animation.intervals);
            return intervals;
        }

        [Test]
        public void ReaderFindsTheIntervalsOfTheAnimationThatCarriesThem()
        {
            var animations = GlbIntervalReader.Read(Glb(Withdraw));
            Assert.AreEqual(1, animations.Count);
            var animation = animations[0];
            Assert.AreEqual("withdraw", animation.name);
            Assert.AreEqual(30f, animation.frameRate);
            Assert.AreEqual(15, animation.length);
            Assert.AreEqual(0.5f, animation.duration, 1e-6f);
            Assert.AreEqual(2, animation.intervals.Count);

            var first = animation.intervals[0];
            Assert.AreEqual("uncontrollable", first.name);
            Assert.AreEqual(0, first.startFrame);
            Assert.AreEqual(10, first.endFrame);
            Assert.AreEqual(10f / 30f, first.endTime, 1e-6f);
            Assert.IsFalse(first.underControl);
            Assert.IsFalse(first.cancellable);

            var second = animation.intervals[1];
            Assert.AreEqual("可取消", second.name);
            Assert.AreEqual(10, second.startFrame);
            Assert.AreEqual(15, second.endFrame);
            Assert.IsTrue(second.underControl);
            Assert.IsTrue(second.cancellable);
        }

        [Test]
        public void GlbWithoutIntervalsYieldsNothing()
        {
            Assert.IsEmpty(GlbIntervalReader.Read(Glb("{\"asset\":{\"version\":\"2.0\"},\"animations\":[{\"name\":\"plain\"}]}")));
            Assert.IsEmpty(GlbIntervalReader.Read(Glb("{\"asset\":{\"version\":\"2.0\"}}")));
            Assert.IsEmpty(GlbIntervalReader.Read(Glb("{\"animations\":[{\"name\":\"a\",\"extras\":{\"other\":1}}]}")));
        }

        [Test]
        public void UnreadableOrDamagedFilesYieldNothingAndDoNotThrow()
        {
            Assert.IsEmpty(GlbIntervalReader.Read(null));
            Assert.IsEmpty(GlbIntervalReader.Read(new byte[] { 1, 2, 3 }));
            Assert.IsEmpty(GlbIntervalReader.Read(Encoding.UTF8.GetBytes("not a model")));
            Assert.IsEmpty(GlbIntervalReader.Read(Glb("{\"animations\":[{\"extras\":{\"yu5h1\":{\"intervals\":{\"fps\":30,\"length\":15,\"items\":\"broken\"}}}}]}")));
            var truncated = Glb(Withdraw);
            Assert.IsEmpty(GlbIntervalReader.Read(truncated.Take(40).ToArray()));
        }

        [Test]
        public void PlainGltfJsonIsReadToo()
        {
            Assert.AreEqual(1, GlbIntervalReader.Read(Encoding.UTF8.GetBytes(Withdraw)).Count);
        }

        [Test]
        public void IntervalsAnswerNormalizedTimeAndControlFlags()
        {
            var intervals = Made(GlbIntervalReader.Read(Glb(Withdraw))[0]);
            try
            {
                Assert.AreEqual(2, intervals.intervals.Count);
                Assert.IsTrue(intervals.TryFind("uncontrollable", out var locked));
                Assert.AreEqual(0f, intervals.NormalizedStart(locked), 1e-6f);
                Assert.AreEqual(10f / 15f, intervals.NormalizedEnd(locked), 1e-6f);
                Assert.IsFalse(intervals.TryFind("missing", out _));

                Assert.IsFalse(intervals.IsUnderControl(0.1f));
                Assert.IsFalse(intervals.IsCancellable(0.1f));
                Assert.IsTrue(intervals.IsUnderControl(0.4f));
                Assert.IsTrue(intervals.IsCancellable(0.4f));
                Assert.IsTrue(intervals.IsCancellable(0.5f));
                Assert.IsTrue(intervals.AtNormalized(1f).Any(item => item.cancellable));
                Assert.IsFalse(intervals.IsUnderControl(0.6f));
                Assert.AreEqual(1, intervals.AtNormalized(0.5f).Count());
            }
            finally
            {
                Object.DestroyImmediate(intervals);
            }
        }

        [Test]
        public void IntervalBoundaryBelongsToTheLaterInterval()
        {
            var intervals = Made(GlbIntervalReader.Read(Glb(Withdraw))[0]);
            try
            {
                var boundary = 10f / 30f;
                var holding = intervals.At(boundary).ToList();
                Assert.AreEqual(1, holding.Count);
                Assert.AreEqual(10, holding[0].startFrame);
            }
            finally
            {
                Object.DestroyImmediate(intervals);
            }
        }

        [Test]
        public void SettingsWithoutIntervalSourceKeepAnEmptyList()
        {
            var settings = ScriptableObject.CreateInstance<HumanoidImportSettings>();
            try
            {
                Assert.IsNotNull(settings.intervals);
                Assert.IsEmpty(settings.intervals);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
