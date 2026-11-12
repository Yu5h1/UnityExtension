
using System;
using UnityEngine;

namespace Yu5h1Lib.UI
{
    /// <summary>Advances one reply by whole graphemes from cumulative snapshots.</summary>
    internal sealed class ChatReplyReveal
    {
        internal const int DefaultRate = 30;
        internal const int MaxCredit = 8;
        internal const int ThinkingSecondsThreshold = 5;

        private string target = "";
        private int[] boundaries = Array.Empty<int>();
        private int shownCount;
        private float elapsed;
        private double credit;
        private bool complete;

        private int ShownLength => shownCount == 0 ? 0 : boundaries[shownCount - 1];
        internal bool Done => complete && shownCount == boundaries.Length;
        internal bool HasSnapshot => target.Length != 0 || complete;

        internal void Push(string snapshot)
        {
            if (complete || snapshot == target)
                return;
            snapshot ??= "";
            int shownLength = ShownLength;
            int[] next = GraphemeBoundaries.GetEnds(snapshot);
            int matchedBoundary = Array.BinarySearch(next, shownLength);
            if (shownLength > 0 && (matchedBoundary < 0 || snapshot.Length < shownLength ||
                string.CompareOrdinal(target, 0, snapshot, 0, shownLength) != 0))
            {
                shownCount = 0;
                credit = 0;
            }
            else if (shownLength > 0)
                shownCount = matchedBoundary + 1;

            target = snapshot;
            boundaries = next;
        }

        internal void Complete(string snapshot)
        {
            if (snapshot != null)
                Push(snapshot);
            complete = true;
        }

        internal string Advance(float dt, float rate, float pace, bool reduceMotion, bool showSeconds)
        {
            dt = Mathf.Max(0, dt);
            elapsed += dt;
            int limit = boundaries.Length;
            if (!complete && (limit > 1 || limit == 1 && char.IsHighSurrogate(target[target.Length - 1])))
                limit--;
            limit = Mathf.Max(shownCount, limit);

            if (reduceMotion)
            {
                shownCount = limit;
                credit = 0;
            }
            else if (target.Length > 0)
            {
                credit = Math.Min(MaxCredit, credit + (double)dt * rate * Mathf.Clamp(pace, .1f, 1f));
                int count = Math.Min(limit - shownCount, (int)Math.Floor(credit + .000001d));
                shownCount += count;
                credit = Math.Max(0d, credit - count);
            }

            if (HasSnapshot || shownCount != 0)
                return target.Substring(0, ShownLength);

            int seconds = Mathf.FloorToInt(elapsed);
            return showSeconds && seconds >= ThinkingSecondsThreshold
                ? $"思考中...{seconds}s"
                : "思考中" + new string('.', seconds % 3 + 1);
        }
    }
}
