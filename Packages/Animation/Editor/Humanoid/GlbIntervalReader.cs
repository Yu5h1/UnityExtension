using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Reads the control intervals the Character Builder writes into a motion GLB: for each glTF animation,
    ///   <c>extras.yu5h1.intervals</c> holds <c>fps</c>, <c>length</c> (the last frame), <c>duration</c> and
    ///   <c>items</c> with <c>name</c>, <c>start</c>, <c>end</c> (frames), <c>startTime</c>, <c>endTime</c> (seconds),
    ///   <c>underControl</c> and <c>cancellable</c>. glTFast does not expose animation extras, so the importer reads
    ///   the file's JSON chunk itself. Frames are authoritative: seconds are recomputed as frame over <c>fps</c>. A
    ///   file that is not a GLB or glTF, has no such extras, or has damaged extras yields no intervals and never an
    ///   error, so a GLB without intervals imports as before.
    /// </summary>
    public static class GlbIntervalReader
    {
        public sealed class Animation
        {
            public string name;
            public float frameRate;
            public int length;
            public float duration;
            public readonly List<MotionInterval> intervals = new List<MotionInterval>();
        }

        const uint GlbMagic = 0x46546c67;
        const uint JsonChunk = 0x4e4f534a;

        [Serializable] class Document { public Entry[] animations; }
        [Serializable] class Entry { public string name; public Extras extras; }
        [Serializable] class Extras { public Yu5h1 yu5h1; }
        [Serializable] class Yu5h1 { public Intervals intervals; }
        [Serializable] class Intervals { public float fps; public int length; public float duration; public Item[] items; }
        [Serializable] class Item
        {
            public string name;
            public int start;
            public int end;
            public bool underControl;
            public bool cancellable;
        }

        /// <summary>The animations of a GLB or glTF file that carry intervals, in file order.</summary>
        public static List<Animation> Read(byte[] bytes)
        {
            var result = new List<Animation>();
            var json = JsonOf(bytes);
            if (json == null)
                return result;
            Document document;
            try
            {
                document = JsonUtility.FromJson<Document>(json);
            }
            catch (ArgumentException)
            {
                return result;
            }
            if (document?.animations == null)
                return result;
            foreach (var entry in document.animations)
            {
                var intervals = entry?.extras?.yu5h1?.intervals;
                if (intervals?.items == null || intervals.items.Length == 0 || intervals.fps <= 0f || intervals.length <= 0)
                    continue;
                var animation = new Animation
                {
                    name = entry.name,
                    frameRate = intervals.fps,
                    length = intervals.length,
                    duration = intervals.length / intervals.fps
                };
                foreach (var item in intervals.items)
                {
                    animation.intervals.Add(new MotionInterval
                    {
                        name = item.name,
                        startFrame = item.start,
                        endFrame = item.end,
                        startTime = item.start / intervals.fps,
                        endTime = item.end / intervals.fps,
                        underControl = item.underControl,
                        cancellable = item.cancellable
                    });
                }
                result.Add(animation);
            }
            return result;
        }

        static string JsonOf(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4)
                return null;
            if (BitConverter.ToUInt32(bytes, 0) != GlbMagic)
                return bytes[0] == (byte)'{' ? Encoding.UTF8.GetString(bytes) : null;
            if (bytes.Length < 20 || BitConverter.ToUInt32(bytes, 16) != JsonChunk)
                return null;
            var length = BitConverter.ToUInt32(bytes, 12);
            if (20L + length > bytes.Length)
                return null;
            return Encoding.UTF8.GetString(bytes, 20, (int)length);
        }
    }
}
