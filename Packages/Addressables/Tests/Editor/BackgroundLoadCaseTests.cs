using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Yu5h1Lib.AddressableAssets.Tests
{
    /// <summary>
    /// Runs the pinned UX BackgroundLoad neutral cases (<c>Tests/Editor/UX~/BackgroundLoad</c>) against
    /// <see cref="ContentSlot"/>. The adapter only translates: appStart → Start(prepare.first), select → Select,
    /// progress → Report, ready/fail → Complete. Expected fields the slot cannot observe (focusMoved, blockingOverlay,
    /// hostInputAvailable) belong to the host's interface and are checked manually (BGL-M1), not here.
    /// </summary>
    public class BackgroundLoadCaseTests
    {
        private const string SpecHash = "371eb2142d374ecf59ed6f71ea9dd0e6c0b541a56c4b9945d404a0f64f7a29a2";
        private const string DesignHash = "133879012950e247a23ae80af8425d9d81abbbb88b7620b334da8675bb25bd92";
        private const string CasesHash = "b6f4f74f5aa4e21e27e32e86a2029fc3b283bbf70336612b393aa2d9f54f59d1";

        private static readonly HashSet<string> InterfaceOnly = new HashSet<string> { "focusMoved", "blockingOverlay", "hostInputAvailable" };

        private static string SnapshotPath(string file)
        {
            var package = PackageInfo.FindForAssembly(typeof(BackgroundLoadCaseTests).Assembly);
            Assert.IsNotNull(package, "com.yu5h1.addressables package not found");
            return Path.Combine(package.resolvedPath, "Tests", "Editor", "UX~", "BackgroundLoad", file);
        }

        private static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            return string.Concat(Array.ConvertAll(sha.ComputeHash(File.ReadAllBytes(path)), b => b.ToString("x2")));
        }

        [Test]
        public void SnapshotMatchesThePinnedHashes()
        {
            Assert.AreEqual(SpecHash, Sha256(SnapshotPath("BackgroundLoad.md")));
            Assert.AreEqual(DesignHash, Sha256(SnapshotPath("background-load-design.json")));
            Assert.AreEqual(CasesHash, Sha256(SnapshotPath("background-load-cases.json")));
        }

        private static IEnumerable<TestCaseData> Cases()
        {
            var file = (Dictionary<string, object>)Json.Parse(File.ReadAllText(SnapshotPath("background-load-cases.json")));
            foreach (Dictionary<string, object> item in (List<object>)file["cases"])
                yield return new TestCaseData(item).SetName("BGL case " + item["id"]);
        }

        [TestCaseSource(nameof(Cases))]
        public void NeutralCase(Dictionary<string, object> item)
        {
            var initial = (Dictionary<string, object>)item["initial"];
            var contents = ((List<object>)initial["contents"]).Cast<string>();
            string first = null;
            if (initial.TryGetValue("prepare", out var prepare))
                first = (string)((Dictionary<string, object>)prepare)["first"];
            var slot = new ContentSlot(contents);

            var index = 0;
            foreach (Dictionary<string, object> step in (List<object>)item["steps"])
            {
                index++;
                var action = (Dictionary<string, object>)step["action"];
                var key = action.TryGetValue("key", out var k) ? (string)k : null;
                var startedBefore = slot.loadsStarted.Count;
                switch ((string)action["op"])
                {
                    case "appStart": slot.Start(first); break;
                    case "select": slot.Select(key); break;
                    case "progress": slot.Report(key, action["ratio"] is double ratio ? (float)ratio : (float?)null); break;
                    case "ready": slot.Complete(key, true); break;
                    case "fail": slot.Complete(key, false); break;
                    default: Assert.Fail($"unknown op {action["op"]}"); break;
                }

                var at = $"{item["id"]} step {index} ({action["op"]} {key})";
                foreach (var pair in (Dictionary<string, object>)step["expect"])
                {
                    if (InterfaceOnly.Contains(pair.Key))
                        continue;
                    switch (pair.Key)
                    {
                        case "slot":
                            Assert.AreEqual(pair.Value, slot.shown, at + " slot");
                            break;
                        case "indicator":
                            Assert.AreEqual(IndicatorText((Dictionary<string, object>)pair.Value), IndicatorText(slot), at + " indicator");
                            break;
                        case "loadsStarted":
                            CollectionAssert.AreEqual((List<object>)pair.Value, slot.loadsStarted, at + " loadsStarted");
                            break;
                        case "fallbackNotice":
                            Assert.AreEqual(pair.Value, slot.noticeVisible, at + " fallbackNotice");
                            break;
                        case "loadRestarted":
                            var restarted = slot.loadsStarted.Count > startedBefore && slot.loadsStarted[^1] == key;
                            Assert.AreEqual(pair.Value, restarted, at + " loadRestarted");
                            break;
                        default:
                            Assert.Fail($"{at}: unknown expect field {pair.Key}");
                            break;
                    }
                }
            }
        }

        private static string IndicatorText(Dictionary<string, object> expected)
            => (bool)expected["visible"] ? (string)expected["text"] : "hidden";

        private static string IndicatorText(ContentSlot slot) => slot.indicator switch
        {
            ContentSlot.Indicator.Hidden => "hidden",
            ContentSlot.Indicator.Indeterminate => "indeterminate",
            ContentSlot.Indicator.Preparing => "preparing",
            _ => slot.percent.ToString(CultureInfo.InvariantCulture) + "%",
        };

        /// <summary>Just enough JSON for the case file: objects, arrays, strings, numbers, true/false/null.</summary>
        private static class Json
        {
            public static object Parse(string text)
            {
                var i = 0;
                var value = Value(text, ref i);
                return value;
            }

            private static object Value(string s, ref int i)
            {
                Skip(s, ref i);
                switch (s[i])
                {
                    case '{':
                        var obj = new Dictionary<string, object>();
                        i++;
                        while (true)
                        {
                            Skip(s, ref i);
                            if (s[i] == '}') { i++; return obj; }
                            var name = (string)Value(s, ref i);
                            Skip(s, ref i);
                            i++; // ':'
                            obj[name] = Value(s, ref i);
                            Skip(s, ref i);
                            if (s[i] == ',') i++;
                        }
                    case '[':
                        var list = new List<object>();
                        i++;
                        while (true)
                        {
                            Skip(s, ref i);
                            if (s[i] == ']') { i++; return list; }
                            list.Add(Value(s, ref i));
                            Skip(s, ref i);
                            if (s[i] == ',') i++;
                        }
                    case '"':
                        var sb = new StringBuilder();
                        i++;
                        while (s[i] != '"')
                        {
                            if (s[i] == '\\')
                            {
                                i++;
                                sb.Append(s[i] == 'u' ? (char)Convert.ToInt32(s.Substring(i + 1, 4), 16) : s[i] == 'n' ? '\n' : s[i]);
                                if (s[i] == 'u') i += 4;
                            }
                            else
                                sb.Append(s[i]);
                            i++;
                        }
                        i++;
                        return sb.ToString();
                    default:
                        if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
                        if (s.Substring(i).StartsWith("false")) { i += 5; return false; }
                        if (s.Substring(i).StartsWith("null")) { i += 4; return null; }
                        var start = i;
                        while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                        return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
                }
            }

            private static void Skip(string s, ref int i)
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }
        }
    }
}
