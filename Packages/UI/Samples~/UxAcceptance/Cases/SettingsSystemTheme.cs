// Reads and watches the system app color scheme for the settings acceptance case.
using System;
using System.Collections;
#if UNITY_EDITOR_WIN || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
using System.Runtime.InteropServices;
#endif
using UnityEngine;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Supplies the system app color scheme to the settings case.</summary>
    internal static class SettingsSystemTheme
    {
        private const float PollInterval = 0.2f;

#if UNITY_EDITOR_WIN || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
        private static readonly IntPtr CurrentUser = new IntPtr(unchecked((int)0x80000001));
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string AppsUseLightTheme = "AppsUseLightTheme";
        private const uint RestrictToDword = 0x00000010;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int RegGetValueW(IntPtr key, string subKey, string valueName, uint flags,
            IntPtr valueType, out uint data, ref uint dataSize);
#endif

        /// <summary>Returns true for dark, false for light, or null when the system scheme is unavailable.</summary>
        internal static bool? Read()
        {
#if UNITY_EDITOR_WIN || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
            uint dataSize = sizeof(uint);
            int result = RegGetValueW(CurrentUser, PersonalizeKey, AppsUseLightTheme, RestrictToDword,
                IntPtr.Zero, out uint value, ref dataSize);
            if (result == 0 && dataSize == sizeof(uint) && value <= 1)
                return value == 0;
#endif
            return null;
        }

        /// <summary>Reports changes from the initial scheme on a real-time Unity coroutine.</summary>
        internal static IEnumerator Watch(Func<bool?> read, Action<bool?> onChanged, bool? initial)
        {
            var delay = new WaitForSecondsRealtime(PollInterval);
            bool? previous = initial;
            while (true)
            {
                yield return delay;
                bool? current = read();
                if (current == previous)
                    continue;

                previous = current;
                onChanged(current);
            }
        }
    }
}
