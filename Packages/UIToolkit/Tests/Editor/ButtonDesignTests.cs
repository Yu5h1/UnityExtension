using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// Checks the pinned UX Button snapshot in <c>Tests/Editor/UX~/Button</c> against the hashes recorded in
    /// the acceptance record, and <see cref="ButtonDesign"/> against the snapshot's design data, so a value
    /// copied into C# cannot drift from the UX delivery it claims to come from.
    /// </summary>
    public class ButtonDesignTests
    {
        private const string SpecHash = "3633818b362a902fbd53de13188b7757bf01c9db52339d332697f304b25e71c3";
        private const string DesignHash = "8082bb56a9eb493e9c9d338527659efa389d8b01817a72f4aec8b535e2974075";

        private static string SnapshotPath(string file)
        {
            var package = PackageInfo.FindForAssembly(typeof(ButtonDesign).Assembly);
            Assert.IsNotNull(package, "Yu5h1Lib.UIToolkit package not found");
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
        public void SnapshotIsButton110WithDesign200()
        {
            StringAssert.Contains("Specification version: 1.1.0", File.ReadAllText(SnapshotPath("Button.md")));
            StringAssert.Contains("\"contractVersion\": \"2.0.0\"", File.ReadAllText(SnapshotPath("button-design.json")));
        }

        [Test]
        public void ConstantsMatchTheDesignData()
        {
            var json = File.ReadAllText(SnapshotPath("button-design.json"));
            Assert.AreEqual(Token(json, "size"), ButtonDesign.Size);
            Assert.AreEqual(Token(json, "iconRatio"), ButtonDesign.IconRatio);
            Assert.AreEqual(Token(json, "iconWithLabelSize"), ButtonDesign.IconWithLabelSize);
            Assert.AreEqual(Token(json, "iconWithLabelSize", "labelFontSize"), ButtonDesign.LabelFontSize);
            Assert.AreEqual(Token(json, "iconLabelGap"), ButtonDesign.IconLabelGap);
        }
    }
}
