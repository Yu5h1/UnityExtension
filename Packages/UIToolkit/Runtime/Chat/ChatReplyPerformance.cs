using System;
using System.Globalization;
using UnityEngine;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Reveals a reply's text at a steady pace, fed by cumulative snapshots - the full text so far, never a
    /// delta - whether it arrives in one <see cref="Complete"/> call or many <see cref="Push"/> calls first.
    /// One interface either way: a source that never streams still reveals through the same pace, it just
    /// never has to wait between pushes.
    /// <para>
    /// Steps by whole text elements (<see cref="StringInfo.ParseCombiningCharacters"/>, plus explicit
    /// handling for a regional-indicator flag pair and a base character followed by ZWJ or a skin-tone
    /// modifier), so an emoji or combining mark is never split mid-reveal.
    /// </para>
    /// <para>No <c>VisualElement</c> - <see cref="Tick"/> returns the string to show; a presenter decides
    /// what to do with it.</para>
    /// </summary>
    public sealed class ChatReplyPerformance
    {
        private string target = "";
        private int shown;
        private int[] boundaries = Array.Empty<int>();
        private float elapsed, credit;
        private bool complete;

        /// <summary>True once every boundary pushed has been revealed and <see cref="Complete"/> was called.</summary>
        public bool Done => complete && shown == target.Length;

        /// <summary>True from the first frame any non-empty text has actually been shown - never set back to
        /// false by this class. A caller with its own idea of "reply arrived" (an ambient animation, a
        /// sound) reads this instead of duplicating the reveal's own bookkeeping.</summary>
        public bool HasRevealedText { get; private set; }

        /// <summary>Back to a fresh reply - clears everything <see cref="Push"/>/<see cref="Complete"/> built up.</summary>
        public void Begin()
        {
            target = "";
            shown = 0;
            boundaries = Array.Empty<int>();
            elapsed = credit = 0;
            complete = false;
            HasRevealedText = false;
        }

        /// <summary>
        /// A new cumulative snapshot. Resets the reveal to the start only when the already-revealed prefix
        /// no longer matches - a genuinely different reply, not the same one growing - so a snapshot whose
        /// unrevealed tail changed (still normal mid-stream) does not restart what the reader has already seen.
        /// </summary>
        public void Push(string snapshot)
        {
            snapshot ??= "";
            if (snapshot == target) return;
            if (!snapshot.StartsWith(target.Substring(0, shown), StringComparison.Ordinal)) shown = 0;
            target = snapshot;

            boundaries = GraphemeBoundaries.GetEnds(target);
        }

        /// <summary>The final text. Still reveals at the normal pace - <see cref="Done"/> tells the caller
        /// once the reveal has caught up to it, which is what "finished quickly" means here, not an instant
        /// jump.</summary>
        public void Complete(string final)
        {
            Push(final);
            complete = true;
        }

        /// <summary>Jumps straight to the fully revealed text without waiting out the pace - for a caller
        /// that scrolled away (or otherwise wants the whole thing now) to catch up on demand. Before
        /// <see cref="Complete"/>, that is only whatever has actually been pushed so far.</summary>
        public string RevealComplete()
        {
            if (!complete) return target.Substring(0, shown);
            shown = target.Length;
            HasRevealedText = !string.IsNullOrWhiteSpace(target);
            return target;
        }

        /// <summary>"思考中" with a cycling 1-3 dots, plus the elapsed whole seconds once past 5 - shown
        /// whenever there is nothing revealed yet and the reply is not done.</summary>
        private string Waiting()
        {
            int seconds = (int)elapsed;
            return "思考中" + new string('.', seconds % 3 + 1) + (seconds >= 5 ? seconds + "s" : "");
        }

        /// <summary>
        /// Advances by <paramref name="dt"/> and returns what should be displayed this frame: the thinking
        /// placeholder while nothing has arrived yet, or the text revealed so far. <paramref name="reduceMotion"/>
        /// skips the pacing and reveals everything pushed so far immediately, matching every other
        /// <c>Tick(dt, reduceMotion, …)</c> in this package. <paramref name="pace"/> scales the reveal rate,
        /// clamped to [0.1, 1].
        /// </summary>
        public string Tick(float dt, bool reduceMotion, float pace = 1f)
        {
            elapsed += Math.Max(0, dt);
            if (target.Length == 0) return complete ? "" : Waiting();

            credit = Math.Min(8, credit + Math.Max(0, dt) * 30 * Mathf.Clamp(pace, 0.1f, 1f));
            // Never reveal past the second-to-last boundary until Complete() confirms there is no more text
            // coming - the last text element of a mid-stream snapshot may still be cut short by the next one.
            int limit = complete ? target.Length : boundaries.Length > 1 ? boundaries[boundaries.Length - 2] : 0;
            foreach (int end in boundaries)
            {
                if (end <= shown) continue;
                if (end > limit || !reduceMotion && credit < 1) break;
                shown = end;
                credit = Math.Max(0, credit - 1);
            }
            if (shown > 0 && !string.IsNullOrWhiteSpace(target.Substring(0, shown))) HasRevealedText = true;
            return shown == 0 && !complete ? Waiting() : target.Substring(0, shown);
        }
    }
}
