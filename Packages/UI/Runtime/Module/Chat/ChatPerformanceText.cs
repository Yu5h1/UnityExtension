using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>Plays chat effects on generated vertices while keeping the original label layout.</summary>
    internal sealed class ChatPerformanceText : Text
    {
        private readonly struct Effect
        {
            internal readonly float Start, Duration;
            internal readonly Vector2 Offset;

            internal Effect(float start, float duration, Vector2 offset)
            {
                Start = start;
                Duration = duration;
                Offset = offset;
            }

            internal float Remaining(float time)
            {
                float remaining = 1f - Mathf.Clamp01((time - Start) / Duration);
                return remaining * remaining * remaining;
            }
        }

        private struct EffectLayer
        {
            internal Dictionary<int, Effect> Units;
            internal float Until;
        }

        private EffectLayer fades, moves;
        private ChatTextPerformance previousConfig;
        private System.Random random;
        private TextGenerator prefixGenerator;
        private string mappedText;
        private Font mappedFont;
        private int mappedQuads;
        private Vector2 mappedSize;
        private int[] glyphEnds;
        private int[] ends = Array.Empty<int>();
        private float time;
        private string shown = "";
        private int revision;

        internal bool Playing => time < fades.Until || time < moves.Until;

        internal void ReleaseEffects()
        {
            fades = moves = default;
            glyphEnds = null;
            mappedText = null;
            mappedFont = null;
            prefixGenerator = null;
            ends = Array.Empty<int>();
            shown = "";
        }

        internal void Advance(float dt, string value, bool content, int nextRevision, bool reduced, ChatTextPerformance config)
        {
            bool wasPlaying = Playing;
            time += dt;
            if (!content || reduced || revision != nextRevision || !value.StartsWith(shown, StringComparison.Ordinal))
                ReleaseEffects();
            revision = nextRevision;

            bool fadeSequential = !config.Instant && config.FadeSequential;
            bool moveSequential = !config.Instant && config.MoveSequential;
            bool fadeChanged = config.EffectChanged(previousConfig, false);
            bool moveChanged = config.EffectChanged(previousConfig, true);
            previousConfig = config;
            if (fadeChanged || time >= fades.Until)
                fades = default;
            if (moveChanged || time >= moves.Until)
                moves = default;

            if (content)
            {
                int oldLength = shown.Length;
                if (value != shown)
                {
                    shown = value;
                    ends = GraphemeBoundaries.GetEnds(value);
                }

                if (!reduced && (config.Fade || config.Move))
                    StartEffects(config, oldLength, fadeChanged, moveChanged, fadeSequential, moveSequential);
            }

            if (wasPlaying || Playing)
                SetVerticesDirty();
        }

        private void StartEffects(ChatTextPerformance config, int oldLength, bool fadeChanged, bool moveChanged,
            bool fadeSequential, bool moveSequential)
        {
            int fadeFrom = config.Fade ? fadeChanged ? 0 : oldLength : shown.Length;
            int moveFrom = config.Move ? moveChanged ? 0 : oldLength : shown.Length;
            int start = Math.Min(fadeFrom, moveFrom);
            if (start >= shown.Length)
                return;

            int first = start == 0 ? 0 : Array.BinarySearch(ends, start) + 1;
            for (int i = first; i < ends.Length; start = ends[i++])
            {
                if (char.IsWhiteSpace(shown, start))
                    continue;

                int predecessor = WordPredecessor(i, start);
                if (start >= fadeFrom)
                    Track(ref fades, config, false, start, fadeSequential ? start : predecessor);
                if (start >= moveFrom)
                    Track(ref moves, config, true, start, moveSequential ? start : predecessor);
            }
        }

        /// <summary>Groups conventional words between separators and falls back to whole graphemes in unspaced scripts.</summary>
        private int WordPredecessor(int index, int start)
        {
            if (index == 0)
                return start;

            int previous = index > 1 ? ends[index - 2] : 0;
            if (IsWordLetter(shown, previous) && (IsWordLetter(shown, start) || IsApostrophe(shown[start])))
                return previous;
            if (index > 1 && IsApostrophe(shown[previous]) && IsWordLetter(shown, start) &&
                IsWordLetter(shown, index > 2 ? ends[index - 3] : 0))
                return previous;
            return start;
        }

        private static bool IsApostrophe(char value) => value is '\'' or '\u2019';

        private static bool IsWordLetter(string value, int index)
        {
            int code = char.IsSurrogatePair(value, index) ? char.ConvertToUtf32(value, index) : value[index];
            if (code is >= 0x0e00 and <= 0x0eff or >= 0x1000 and <= 0x109f or >= 0x1780 and <= 0x17ff or
                >= 0x3040 and <= 0x30ff or >= 0x31f0 and <= 0x31ff or >= 0x3400 and <= 0x9fff or
                >= 0xa9e0 and <= 0xa9ff or >= 0xaa60 and <= 0xaa7f or >= 0xf900 and <= 0xfaff or
                >= 0xff66 and <= 0xff9f or >= 0x1aff0 and <= 0x1b16f or >= 0x20000 and <= 0x323af)
                return false;

            var category = CharUnicodeInfo.GetUnicodeCategory(value, index);
            return category is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or
                UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or
                UnicodeCategory.DecimalDigitNumber or UnicodeCategory.LetterNumber;
        }

        private void Track(ref EffectLayer layer, ChatTextPerformance config, bool move, int start, int previous)
        {
            Effect effect = default;
            if (start == previous)
                effect = Sample(config, move);
            else
                layer.Units?.TryGetValue(previous, out effect);

            if (effect.Duration <= 0f)
                return;
            (layer.Units ??= new())[start] = effect;
            layer.Until = Mathf.Max(layer.Until, effect.Start + effect.Duration);
        }

        private Effect Sample(ChatTextPerformance config, bool move)
        {
            if (config.Percentage <= 0f || config.DurationMax <= 0f ||
                config.Percentage < 100f && SampleUnit() * 100f >= config.Percentage)
                return default;

            float duration = Mathf.Lerp(config.DurationMin, config.DurationMax, SampleUnit());
            if (duration <= 0f)
                return default;

            Vector2 offset = Vector2.zero;
            if (move)
            {
                float sample = SampleUnit();
                float distance = 0f;
                if (config.Directed)
                    distance = Mathf.Lerp(config.DistanceMin, config.DistanceMax, sample);
                else if (config.DistanceMax > 0f)
                {
                    float inner = config.DistanceMin / config.DistanceMax;
                    float innerSquared = inner * inner;
                    distance = config.DistanceMax * Mathf.Sqrt(Mathf.Lerp(innerSquared, 1f, sample));
                }
                float angle = (config.Directed ? config.DirectionDegrees % 360f :
                    SampleUnit() * 360f) * Mathf.Deg2Rad;
                offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                if (distance == 0f)
                    return default;
            }

            return new Effect(time, duration, offset);
        }

        private float SampleUnit() => (float)(random ??= new System.Random()).NextDouble();

        /// <summary>Uses the native generator to account for whitespace omissions, missing glyphs and combined glyphs.</summary>
        private int[] MapGraphemes(int[] ends, int quads)
        {
            var size = rectTransform.rect.size;
            if (mappedText == shown && mappedFont == font && mappedQuads == quads && mappedSize == size)
                return glyphEnds;
            mappedText = shown;
            mappedFont = font;
            mappedQuads = quads;
            mappedSize = size;
            glyphEnds = null;
            var mapped = new int[ends.Length];
            bool singleGlyphUnits = true;
            int visible = 0;
            for (int i = 0, start = 0; i < ends.Length; start = ends[i++])
            {
                char unit = shown[start];
                singleGlyphUnits &= ends[i] == start + 1 &&
                    (unit <= 127 || unit is >= '\u3000' and <= '\u30ff' or >= '\u3400' and <= '\u9fff' or
                        >= '\uff01' and <= '\uff60');
                if (!char.IsWhiteSpace(shown, start))
                    visible++;
                mapped[i] = visible;
            }
            if (singleGlyphUnits && visible == quads)
                return glyphEnds = mapped;
            if (singleGlyphUnits && ends.Length == quads)
                return glyphEnds = ends;

            var settings = GetGenerationSettings(size);
            prefixGenerator ??= new TextGenerator();
            int previous = 0;
            for (int i = 0; i < ends.Length; i++)
            {
                if (!prefixGenerator.Populate(shown.Substring(0, ends[i]), settings))
                    return null;
                mapped[i] = prefixGenerator.verts.Count / 4;
                if (mapped[i] < previous || mapped[i] > quads)
                    return null;
                previous = mapped[i];
            }
            return previous == quads ? glyphEnds = mapped : null;
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            base.OnPopulateMesh(vertices);
            if (!Playing || shown.Length == 0 || shown != text)
                return;

            int quads = vertices.currentVertCount / 4;
            var mapped = MapGraphemes(ends, quads);
            if (mapped == null || quads == 0)
            {
                fades = moves = default;
                return;
            }

            var vertex = default(UIVertex);
            for (int i = 0, start = 0; i < ends.Length; start = ends[i++])
            {
                float opacity = fades.Units != null && fades.Units.TryGetValue(start, out var fade)
                    ? 1f - fade.Remaining(time) : 1f;
                Vector2 shift = moves.Units != null && moves.Units.TryGetValue(start, out var move)
                    ? move.Offset * move.Remaining(time) : Vector2.zero;
                int first = i == 0 ? 0 : mapped[i - 1];
                int end = mapped[i];
                for (int j = first * 4; j < end * 4; j++)
                {
                    vertices.PopulateUIVertex(ref vertex, j);
                    vertex.position += (Vector3)shift;
                    var tint = vertex.color;
                    tint.a = (byte)Mathf.RoundToInt(tint.a * opacity);
                    vertex.color = tint;
                    vertices.SetUIVertex(vertex, j);
                }
            }
        }

    }
}
