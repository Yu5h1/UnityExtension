using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// Checks the pinned UX OptionPicker snapshot in <c>Tests/Editor/UX~/OptionPicker</c> against the hashes recorded in
    /// the acceptance record, and verifies that the snapshot matches the 3.0.2 contract and design data (2.0.0).
    /// </summary>
    public class OptionPickerDesignTests
    {
        private const string SpecHash = "2adff90e313ce7c09041dc98d6696e9f9f278f4c9329ca9a4626a13ac44400b9";
        private const string DesignHash = "551517bc380470057011e41ca7e8366af7dade1f432e3a1cd60134a629417e5a";
        private const string CasesHash = "3d4d2a26a63e603267ac360f0f2a3ee6c8893a7bfb5e924b87b96dac0b58ccd4";

        private static string SnapshotPath(string file)
        {
            var package = PackageInfo.FindForAssembly(typeof(OptionPickerDesignTests).Assembly);
            Assert.IsNotNull(package, "Yu5h1Lib.UIToolkit package not found");
            return Path.Combine(package.resolvedPath, "Tests", "Editor", "UX~", "OptionPicker", file);
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
            Assert.IsTrue(match.Success, $"{name}.{field} missing from option-picker-design.json");
            return float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        [Test]
        public void SnapshotMatchesThePinnedHashes()
        {
            Assert.AreEqual(SpecHash, Sha256(SnapshotPath("OptionPicker.md")));
            Assert.AreEqual(DesignHash, Sha256(SnapshotPath("option-picker-design.json")));
            Assert.AreEqual(CasesHash, Sha256(SnapshotPath("option-picker-cases.json")));
        }

        [Test]
        public void SnapshotIsOptionPicker310WithDesign200()
        {
            StringAssert.Contains("Specification version: 3.1.0", File.ReadAllText(SnapshotPath("OptionPicker.md")));
            StringAssert.Contains("\"contractVersion\": \"2.0.0\"", File.ReadAllText(SnapshotPath("option-picker-design.json")));
        }

        [Test]
        public void DesignDataTokensArePresent()
        {
            var json = File.ReadAllText(SnapshotPath("option-picker-design.json"));
            Assert.AreEqual(4f, Token(json, "pickerGap"));
            Assert.AreEqual(4f, Token(json, "pickerViewportMargin"));
            Assert.AreEqual(4f, Token(json, "panelPadding"));
            Assert.AreEqual(4f, Token(json, "optionGap"));
        }
    }
}
