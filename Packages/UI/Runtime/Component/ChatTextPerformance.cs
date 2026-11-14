using System;
using UnityEngine;

namespace Yu5h1Lib.UI
{
    /// <summary>Controls text reveal and optional glyph effects.</summary>
    [Serializable]
    public struct ChatTextPerformance
    {
        public bool Instant;
        public bool Fade;
        public bool Move;
        public bool FadeSequential;
        public bool MoveSequential;
        public float SecondsPerGrapheme;
        [Range(0, 100)] public float Percentage;
        public float DurationMin, DurationMax;
        public float DistanceMin, DistanceMax;
        public bool Directed;
        public float DirectionDegrees;

        public static ChatTextPerformance Default => new()
        {
            SecondsPerGrapheme = 1f / TextReveal.DefaultRate,
            Percentage = 100f,
            DurationMin = .45f,
            DurationMax = .75f,
            DistanceMax = 16f
        };

        public void Validate()
        {
            if (!float.IsFinite(SecondsPerGrapheme) || SecondsPerGrapheme <= 0f ||
                !float.IsFinite(1f / SecondsPerGrapheme) ||
                !float.IsFinite(Percentage) || Percentage < 0f || Percentage > 100f ||
                !ValidRange(DurationMin, DurationMax) || !ValidRange(DistanceMin, DistanceMax) ||
                !float.IsFinite(DirectionDegrees))
                throw new ArgumentOutOfRangeException(nameof(ChatTextPerformance));
        }

        internal bool EffectChanged(ChatTextPerformance previous, bool move)
        {
            bool enabled = move ? Move : Fade;
            bool wasEnabled = move ? previous.Move : previous.Fade;
            if (!enabled || !wasEnabled)
                return enabled != wasEnabled;

            bool sequential = !Instant && (move ? MoveSequential : FadeSequential);
            bool wasSequential = !previous.Instant && (move ? previous.MoveSequential : previous.FadeSequential);
            return sequential != wasSequential || Percentage != previous.Percentage ||
                DurationMin != previous.DurationMin || DurationMax != previous.DurationMax ||
                move && (DistanceMin != previous.DistanceMin || DistanceMax != previous.DistanceMax ||
                    Directed != previous.Directed || Directed && DirectionDegrees != previous.DirectionDegrees);
        }

        private static bool ValidRange(float min, float max) =>
            float.IsFinite(min) && float.IsFinite(max) && min >= 0f && max >= min;
    }
}
