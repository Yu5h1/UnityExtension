using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Runs wobbles on whatever elements are currently wobbling, and gets out of the way once they stop.
    /// <para>
    /// One element, one wobble: playing again on an element already rattling builds on the swing it has
    /// rather than starting a second one over the top. That is what makes a flurry of catches read as a
    /// flurry instead of a fight.
    /// </para>
    /// <para>
    /// Every element it touches is handed back exactly as it was found. An element that leaves the panel
    /// mid-wobble is simply dropped - there is nothing left to restore it to.
    /// </para>
    /// </summary>
    public sealed class ShakePlayer
    {
        private readonly List<(ElementTarget Target, Reaction.Shake Shake)> playing =
            new List<(ElementTarget, Reaction.Shake)>();

        /// <summary>How many elements are wobbling right now.</summary>
        public int Count => playing.Count;

        /// <summary>
        /// Starts a wobble on <paramref name="element"/>.
        /// </summary>
        /// <param name="reduceMotion">
        /// When the platform asks for reduced motion nothing plays at all. Whether the user asked for that
        /// is the caller's to know.
        /// </param>
        public void Play(VisualElement element, Reaction.Shake.Settings settings, bool reduceMotion)
        {
            if (element == null || reduceMotion) return;

            for (int i = 0; i < playing.Count; i++)
                if (playing[i].Target.Element == element) { playing[i].Shake.Play(settings); return; }

            var target = new ElementTarget(element);
            var shake = new Reaction.Shake(target);
            playing.Add((target, shake));
            shake.Play(settings);
        }

        /// <summary>Advances every wobble, restoring and dropping the ones that are finished.</summary>
        public void Tick(float deltaTime)
        {
            for (int i = playing.Count - 1; i >= 0; i--)
            {
                var (target, shake) = playing[i];
                if (!target.IsAlive) { playing.RemoveAt(i); continue; }
                if (!shake.IsMoving) { Retire(i); continue; }
                shake.Tick(deltaTime);
            }
        }

        /// <summary>Ends every wobble at once and hands every element back untouched.</summary>
        public void StopAll()
        {
            for (int i = playing.Count - 1; i >= 0; i--) Retire(i);
        }

        private void Retire(int index)
        {
            var (target, shake) = playing[index];
            shake.Stop();
            target.Restore();
            playing.RemoveAt(index);
        }
    }
}
