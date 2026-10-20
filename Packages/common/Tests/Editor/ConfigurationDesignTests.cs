using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// Pins the UX Configuration specification snapshot in <c>Tests/Editor/UX~/Configuration</c> (UX-UNITYEXTENSION-7,
    /// level 1 of goal 2): the file must match the SHA-256 the UX manifest records for it and carry an accepted major version.
    /// Only the specification file is pinned this round; declarations, the Theme schema and the delivery manifest
    /// belong to later goals.
    /// </summary>
    public class ConfigurationDesignTests
    {
        /// <summary>UX <c>Documentation/ModuleSpec/manifest.json</c> entry for
        /// <c>Documentation/ModuleSpec/Foundation/Configuration.md</c> at UX commit 7aa4275 (specification 1.4.1).</summary>
        private const string SpecHash = "4e99803cd70b040b0e5dd89a34865861672325a09641b1e5f44b237b7cd34d48";

        private static string SnapshotPath(string file)
        {
            var package = PackageInfo.FindForAssembly(typeof(ConfigurationDesignTests).Assembly);
            Assert.IsNotNull(package, "Yu5h1Lib.Common package not found");
            return Path.Combine(package.resolvedPath, "Tests", "Editor", "UX~", "Configuration", file);
        }

        private static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(File.ReadAllBytes(path));
            return string.Concat(System.Array.ConvertAll(hash, b => b.ToString("x2")));
        }

        [Test]
        public void SnapshotMatchesTheUxManifestHash()
        {
            Assert.AreEqual(SpecHash, Sha256(SnapshotPath("Configuration.md")));
        }

        [Test]
        public void SnapshotMajorVersionIsOne()
        {
            var text = File.ReadAllText(SnapshotPath("Configuration.md"));
            var match = Regex.Match(text, @"規範版本：(\d+)\.(\d+)\.(\d+)");
            Assert.IsTrue(match.Success, "Configuration.md has no 規範版本 line");
            Assert.AreEqual("1", match.Groups[1].Value, "Unsupported Configuration major version");
            Assert.AreEqual("1.4.1", $"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}");
        }
    }
}
