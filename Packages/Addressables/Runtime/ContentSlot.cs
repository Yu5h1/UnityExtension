using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// One place on screen that shows one of several contents loaded in the background (a character stage, an avatar):
    /// the Unity implementation of UX BackgroundLoad. It owns the decisions and none of the loading: it asks its host to
    /// load a key (<see cref="loadRequested"/>), the host reports back with <see cref="Report"/> and
    /// <see cref="Complete"/>, and the slot says what the place shows, what the progress indicator reads and whether the
    /// failure notice is up.
    /// <code>
    /// Start(first)  → loads first; once it is ready (or after the first Select) the rest load one at a time
    /// Select(key)   → shows key when ready, otherwise the placeholder while key loads; the newest selection wins
    /// Complete(key) → a ready selection replaces the placeholder; a failed selection shows the fallback and the notice
    /// </code>
    /// A selected key loads at once even while a background load runs; background loads never overlap each other.
    /// </summary>
    public class ContentSlot
    {
        public enum LoadState { NotLoaded, Downloading, Preparing, Ready, Failed }

        /// <summary>What the progress indicator shows; <see cref="percent"/> carries the number for <see cref="Percent"/>.</summary>
        public enum Indicator { Hidden, Percent, Indeterminate, Preparing }

        /// <summary>What the place shows when it is not a ready content.</summary>
        public const string Placeholder = "placeholder";
        public const string Fallback = "fallback";

        private class Entry
        {
            public LoadState state;
            public float? ratio;
            public bool loading;
        }

        private readonly List<string> _keys;
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
        private string _first;
        private string _background;
        private bool _restStarted;

        /// <summary>The host loads the key and calls <see cref="Report"/> and <see cref="Complete"/> for it.</summary>
        public event Action<string> loadRequested;
        /// <summary>Anything shown changed: <see cref="shown"/>, <see cref="indicator"/> or <see cref="noticeVisible"/>.</summary>
        public event Action changed;

        public ContentSlot(IEnumerable<string> keys)
        {
            _keys = new List<string>(keys);
            foreach (var key in _keys)
                _entries[key] = new Entry();
        }

        public IReadOnlyList<string> keys => _keys;

        /// <summary>The selected key; null before the first <see cref="Select"/>.</summary>
        public string selected { get; private set; }

        /// <summary>What the place shows: a ready key, <see cref="Placeholder"/>, <see cref="Fallback"/>, or null.</summary>
        public string shown { get; private set; }

        /// <summary>What the indicator shows. It follows the selected key while that loads, else the background load.</summary>
        public Indicator indicator { get; private set; }

        /// <summary>The download share as a whole percent, rounded down so 100 appears only when the download is done.</summary>
        public int percent { get; private set; }

        /// <summary>The non-blocking notice that the selected content failed; it stays until the next selection.</summary>
        public bool noticeVisible { get; private set; }

        /// <summary>The keys whose loads were requested, in order (a reload after a failure appears again).</summary>
        public List<string> loadsStarted { get; } = new List<string>();

        public LoadState StateOf(string key) => _entries[key].state;

        /// <summary>Starts loading the content the user will most likely see first; null or unknown loads nothing.</summary>
        public void Start(string first)
        {
            if (first == null || !_entries.ContainsKey(first))
                return;
            _first = first;
            RequestBackground(first);
            Refresh();
        }

        /// <summary>The user chose <paramref name="key"/>. Clears the notice, shows it when ready, otherwise loads it.</summary>
        public void Select(string key)
        {
            if (!_entries.TryGetValue(key, out var entry))
                return;
            selected = key;
            noticeVisible = false;
            if (entry.state != LoadState.Ready && !entry.loading)
                RequestLoad(key);
            _restStarted = true;
            ContinueRest();
            Refresh();
        }

        /// <summary>The user left the place (for example its page closed): the failure notice does not outlive that.</summary>
        public void DismissNotice()
        {
            if (!noticeVisible)
                return;
            noticeVisible = false;
            Refresh();
        }

        /// <summary>Progress of a running load: the download share, or null when it is not known.</summary>
        public void Report(string key, float? ratio)
        {
            if (!_entries.TryGetValue(key, out var entry) || !entry.loading)
                return;
            if (ratio.HasValue)
            {
                var value = Mathf.Clamp01(ratio.Value);
                entry.ratio = entry.ratio.HasValue ? Mathf.Max(entry.ratio.Value, value) : value;
                entry.state = entry.ratio >= 1f ? LoadState.Preparing : LoadState.Downloading;
            }
            Refresh();
        }

        /// <summary>A load finished. Only the selected key changes what the place shows.</summary>
        public void Complete(string key, bool success)
        {
            if (!_entries.TryGetValue(key, out var entry) || !entry.loading)
                return;
            entry.loading = false;
            entry.state = success ? LoadState.Ready : LoadState.Failed;
            if (_background == key)
                _background = null;
            if (key == selected && !success)
                noticeVisible = true;
            if (key == _first)
                _restStarted = true;
            ContinueRest();
            Refresh();
        }

        private void RequestBackground(string key)
        {
            _background = key;
            RequestLoad(key);
        }

        private void RequestLoad(string key)
        {
            var entry = _entries[key];
            entry.loading = true;
            entry.ratio = null;
            entry.state = LoadState.Downloading;
            loadsStarted.Add(key);
            loadRequested?.Invoke(key);
        }

        /// <summary>After the first content is ready or chosen, the rest load in declared order while nothing else loads.</summary>
        private void ContinueRest()
        {
            if (!_restStarted || _background != null)
                return;
            foreach (var entry in _entries.Values)
                if (entry.loading)
                    return;
            foreach (var key in _keys)
                if (_entries[key].state == LoadState.NotLoaded)
                {
                    RequestBackground(key);
                    return;
                }
        }

        private void Refresh()
        {
            var selectedEntry = selected != null ? _entries[selected] : null;
            if (selectedEntry != null)
                shown = selectedEntry.state == LoadState.Ready ? selected
                    : selectedEntry.state == LoadState.Failed ? Fallback
                    : Placeholder;

            var followed = selectedEntry != null && selectedEntry.loading ? selectedEntry
                : _background != null ? _entries[_background] : null;
            percent = 0;
            if (followed == null)
                indicator = Indicator.Hidden;
            else if (followed.state == LoadState.Preparing)
                indicator = Indicator.Preparing;
            else if (!followed.ratio.HasValue)
                indicator = Indicator.Indeterminate;
            else
            {
                indicator = Indicator.Percent;
                // The small margin keeps float error (0.29f * 100 = 28.99…) from losing a percent; it never reaches 100.
                percent = Mathf.FloorToInt(followed.ratio.Value * 100f + 0.0001f);
            }
            changed?.Invoke();
        }
    }
}
