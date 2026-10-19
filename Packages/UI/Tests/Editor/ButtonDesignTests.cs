using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>
    /// Checks the pinned UX Button snapshot in <c>Tests/Editor/UX~/Button</c> against the hashes recorded in
    /// the acceptance record, and verifies that the snapshot matches the 2.0.0 contract and design data (3.0.0).
    /// </summary>
    public class ButtonDesignTests
    {
        private const string SpecHash = "9662a105972563ca4b546aac245aa5321761028b7aa96c6de7877f6ed77f2567";
        private const string DesignHash = "e2568af28f704603b2a0d696324f2389a81a685c5ad96049a38c4008cde5c0f9";

        private static string SnapshotPath(string file)
        {
            var package = PackageInfo.FindForAssembly(typeof(ButtonDesignTests).Assembly);
            Assert.IsNotNull(package, "Yu5h1Lib.UI package not found");
            return Path.Combine(package.resolvedPath, "Tests", "Editor", "UX~", "Button", file);
        }

        private static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(File.ReadAllBytes(path));
            return string.Concat(System.Array.ConvertAll(hash, b => b.ToString("x2")));
        }

        private static float Token(string json, string name, string field = "value")
        {
            var match = Regex.Match(json, "\"" + name + "\"\\s*:\\s*\\{[^}]*\"" + field + "\"\\s*:\\s*([0-9.]+)");
            Assert.IsTrue(match.Success, $"{name}.{field} missing from button-design.json");
            return float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        [Test]
        public void SnapshotMatchesThePinnedHashes()
        {
            Assert.AreEqual(SpecHash, Sha256(SnapshotPath("Button.md")));
            Assert.AreEqual(DesignHash, Sha256(SnapshotPath("button-design.json")));
        }

        [Test]
        public void SnapshotIsButton200WithDesign300()
        {
            StringAssert.Contains("Specification version: 2.0.0", File.ReadAllText(SnapshotPath("Button.md")));
            StringAssert.Contains("\"contractVersion\": \"3.0.0\"", File.ReadAllText(SnapshotPath("button-design.json")));
        }

        [Test]
        public void DesignDataTokensArePresent()
        {
            var json = File.ReadAllText(SnapshotPath("button-design.json"));
            Assert.AreEqual(32f, Token(json, "size"));
            Assert.AreEqual(0.75f, Token(json, "iconRatio"));
            Assert.AreEqual(8f, Token(json, "iconLabelGap"));
            Assert.AreEqual(ButtonAddon.DefaultSize, Token(json, "size"));
            Assert.AreEqual(ButtonAddon.IconRatio, Token(json, "iconRatio"));
            Assert.AreEqual(ButtonAddon.IconLabelGap, Token(json, "iconLabelGap"));
        }
    }
}
