using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Yu5h1Lib.UIToolkit.Editor;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// Checks the pinned UX VisualFoundation snapshot in <c>Tests/Editor/UX~/VisualFoundation</c> against the hashes recorded in
    /// the acceptance record, that <c>Yu5h1VisualFoundation.uss</c> is exactly what the generator makes from it, and that the
    /// Button and OptionPicker style sheets reach colour only through the palette variables.
    /// </summary>
    public class VisualFoundationDesignTests
    {
        private const string SpecHash = "7ddc4139f980c279247ad46ae8fbc2f1d1a742be6d5983c856b6dab750999de8";
        private const string DesignHash = "7d41c8287ef9aafe068280459f10cf906f616c803c530e16bb469843940b42c9";
        private const string CasesHash = "06a9edb4e1d450c45718408d897e434cee093cfab248f97aba67eb4a3aaba807";

        private static string PackageFile(params string[] parts) =>
            Path.Combine(new[] { VisualFoundationThemeGenerator.PackagePath() }.Concat(parts).ToArray());

        private static string SnapshotPath(string file) => PackageFile("Tests", "Editor", "UX~", "VisualFoundation", file);

        private static string Sha256(string path) => VisualFoundationThemeGenerator.Sha256(File.ReadAllBytes(path));

        private static string Resource(string file) => PackageFile("Runtime", "Resources", "Yu5h1Lib.UIToolkit", file);

        [Test]
        public void SnapshotMatchesThePinnedHashes()
        {
            Assert.AreEqual(SpecHash, Sha256(SnapshotPath("VisualFoundation.md")));
            Assert.AreEqual(DesignHash, Sha256(SnapshotPath("visual-design.json")));
            Assert.AreEqual(CasesHash, Sha256(SnapshotPath("visual-cases.json")));
        }

        [Test]
        public void SnapshotIsVisualFoundation140WithDesign120()
        {
            StringAssert.Contains("Specification version: 1.4.0", File.ReadAllText(SnapshotPath("VisualFoundation.md")));
            StringAssert.Contains("\"contractVersion\": \"1.2.0\"", File.ReadAllText(SnapshotPath("visual-design.json")));
            StringAssert.Contains("\"schemaVersion\": 1", File.ReadAllText(SnapshotPath("visual-design.json")));
        }

        [Test]
        public void DesignDataDefinesFifteenRolesPerSchemeInTheSameOrder()
        {
            var json = File.ReadAllText(SnapshotPath("visual-design.json"));
            var light = VisualFoundationThemeGenerator.ReadRoles(json, "light").Select(r => r.Key).ToList();
            var dark = VisualFoundationThemeGenerator.ReadRoles(json, "dark").Select(r => r.Key).ToList();
            Assert.AreEqual(15, light.Count);
            CollectionAssert.AreEqual(light, dark);
        }

        [Test]
        public void ThemeStyleSheetIsWhatTheGeneratorMakesFromThePinnedDesign()
        {
            var generated = VisualFoundationThemeGenerator.GenerateFromPinnedDesign();
            Assert.AreEqual(generated, VisualFoundationThemeGenerator.GenerateFromPinnedDesign(), "The same input must give the same output");
            Assert.AreEqual(generated, File.ReadAllText(Resource("Yu5h1VisualFoundation.uss"), Encoding.UTF8).Replace("\r\n", "\n"),
                "Yu5h1VisualFoundation.uss is stale: run Tools > Yu5h1Lib > UIToolkit > Regenerate VisualFoundation Theme");
        }

        [Test]
        public void ThemeCarriesEveryRoleOfBothSchemesAtTheDesignValue()
        {
            var json = File.ReadAllText(SnapshotPath("visual-design.json"));
            var uss = File.ReadAllText(Resource("Yu5h1VisualFoundation.uss"));
            foreach (var scheme in new[] { "light", "dark" })
            {
                var block = Regex.Match(uss, @"\.yu5h1-theme--" + scheme + @" \{(.*?)\n\}", RegexOptions.Singleline).Groups[1].Value;
                foreach (var role in VisualFoundationThemeGenerator.ReadRoles(json, scheme))
                {
                    StringAssert.Contains($"{VisualFoundationThemeGenerator.VariableName(role.Key)}: {role.Value};", block, $"{scheme} {role.Key}");
                }
                StringAssert.Contains($"{VisualFoundationThemeGenerator.FocusWidthVariable}: 2px;", block);
            }
        }

        [TestCase("Yu5h1Button.uss")]
        [TestCase("Yu5h1OptionPicker.uss")]
        public void StyleSheetsReadOnlyVariablesTheThemeDeclares(string file)
        {
            var declared = new HashSet<string>(Regex.Matches(File.ReadAllText(Resource("Yu5h1VisualFoundation.uss")), @"(--yu5h1-[a-z-]+):")
                .Cast<Match>().Select(m => m.Groups[1].Value));
            var used = Regex.Matches(File.ReadAllText(Resource(file)), @"var\((--yu5h1-[a-z-]+)").Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
            Assert.IsNotEmpty(used, file + " should read the palette");
            foreach (var name in used) Assert.IsTrue(declared.Contains(name), $"{file} reads {name}, which the theme does not declare");
        }

        [TestCase("Yu5h1Button.uss")]
        [TestCase("Yu5h1OptionPicker.uss")]
        public void StyleSheetsHaveNoColourLiteral(string file)
        {
            var lines = File.ReadAllLines(Resource(file))
                .Where(l => Regex.IsMatch(l, @"rgb\(|#[0-9a-fA-F]{3,8}\b") && !l.Contains("rgba(0, 0, 0, 0)")).ToList();
            CollectionAssert.IsEmpty(lines, file + " must reach every colour through the palette variables");
        }

        [Test]
        public void FocusRingIsAtLeastFocusMinWidthThick()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(ImageConversion.LoadImage(texture, File.ReadAllBytes(Resource("Yu5h1FocusRing.png"))));
                var minWidth = VisualFoundationThemeGenerator.ReadFocusMinWidth(File.ReadAllText(SnapshotPath("visual-design.json")));
                var middle = texture.height / 2;
                var thickness = 0;
                while (thickness < texture.width && texture.GetPixel(thickness, middle).a > 0.5f) thickness++;
                Assert.GreaterOrEqual(thickness, minWidth, "Opaque ring width at the middle of the left edge");
                Assert.Less(thickness, texture.width / 2, "The ring must leave the middle of the target visible");
                Assert.AreEqual(0f, texture.GetPixel(texture.width / 2, middle).a, "Centre of the ring is transparent");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
