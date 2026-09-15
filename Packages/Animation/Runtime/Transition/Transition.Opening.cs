using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    public static partial class Transition
    {
        /// <summary>
        /// A set of <see cref="Entrance"/>s performed together on one clock, and skippable as one.
        /// <para>
        /// The shared clock is the point. Each item has its own launch and arrival reading, and staggering
        /// only works if they are all read against the same time - give each its own stopwatch and the
        /// choreography drifts apart.
        /// </para>
        /// <para>
        /// An opening is always skippable. Anyone who taps during it has said they want the screen, not the
        /// show, and <see cref="Skip"/> puts everything where it belongs at once without firing arrival
        /// feedback.
        /// </para>
        /// </summary>
        public sealed class Opening
        {
            private readonly List<Entrance> performing = new List<Entrance>();
            private float clock = -1, deadline;

            /// <summary>True while the performance is running.</summary>
            public bool IsPlaying => clock >= 0;

            /// <summary>Seconds since the performance began, or -1 when nothing is running.</summary>
            public float Clock => clock;

            /// <summary>How many items are taking part.</summary>
            public int Count => performing.Count;

            /// <summary>
            /// Begins a performance. Anything already running is skipped first, so two openings can never
            /// overlap and fight over the same items.
            /// </summary>
            /// <param name="timeout">
            /// Seconds after which the performance ends regardless. An item whose destination never gains a
            /// size would otherwise wait for a layout that never comes and hold the opening open forever.
            /// </param>
            public void Begin(IEnumerable<Entrance> entrances, float timeout)
            {
                Skip();
                if (entrances != null) performing.AddRange(entrances);
                if (performing.Count == 0) return;
                clock = 0;
                deadline = Mathf.Max(0, timeout);
            }

            /// <summary>
            /// Advances the shared clock. Returns false once the performance is over, whether every item
            /// arrived or the timeout ended it.
            /// </summary>
            public bool Tick(float deltaTime)
            {
                if (!IsPlaying) return false;
                if (!deltaTime.IsFinite() || deltaTime < 0) return true;

                clock += deltaTime;

                bool running = false;
                foreach (var entrance in performing)
                {
                    entrance.Tick(clock);
                    running |= !entrance.Done;
                }

                if (running && clock < deadline) return true;
                if (running) Skip(); else Clear();
                return false;
            }

            /// <summary>
            /// Ends the performance now, putting every item where it belongs without arrival feedback.
            /// Safe to call when nothing is running.
            /// </summary>
            public void Skip()
            {
                foreach (var entrance in performing) entrance.Finish(false);
                Clear();
            }

            private void Clear()
            {
                performing.Clear();
                clock = -1;
            }
        }
    }
}
