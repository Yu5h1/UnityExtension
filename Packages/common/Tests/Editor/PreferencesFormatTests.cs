using NUnit.Framework;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib.Tests
{
    /// <summary>Configuration level 1, load without overwrite (CFG-02): what counts as a readable save and what is
    /// damaged or of an unknown version, decided offline by <see cref="PreferencesFormat"/>.</summary>
    public class PreferencesFormatTests
    {
        private static DataView Sample()
        {
            var data = new DataView();
            data["count"] = "4";
            data["name"] = "a,b \"c\" \\ \u00e9 \u4e2d";
            return data;
        }

        [Test]
        public void SaveWritesTheVersionEntryAndLoadHidesIt()
        {
            var json = PreferencesFormat.ToJson(Sample());
            StringAssert.Contains("\"$version\"", json);

            var result = PreferencesFormat.Parse(json);
            Assert.AreEqual(PreferencesLoadStatus.Loaded, result.Status, result.Error);
            Assert.IsFalse(result.Data.ContainsKey(PreferencesFormat.VersionField));
            Assert.AreEqual(2, result.Data.Count);
            Assert.AreEqual("4", result.Data["count"]);
            Assert.AreEqual(Sample()["name"], result.Data["name"]);
        }

        [Test]
        public void ToJsonDoesNotChangeTheView()
        {
            var data = Sample();
            PreferencesFormat.ToJson(data);
            Assert.AreEqual(2, data.Count);
            Assert.IsFalse(data.ContainsKey(PreferencesFormat.VersionField));
        }

        [Test]
        public void EmptyViewRoundTrips()
        {
            var result = PreferencesFormat.Parse(PreferencesFormat.ToJson(new DataView()));
            Assert.AreEqual(PreferencesLoadStatus.Loaded, result.Status, result.Error);
            Assert.AreEqual(0, result.Data.Count);
        }

        [TestCase("{\"_entries\":[{\"key\":\"count\",\"value\":\"4\"}]}", Description = "entries format without a version entry")]
        [TestCase("{\"count\":\"4\"}", Description = "flat string object of the oldest writer")]
        [TestCase("  {\"_entries\":[{\"key\":\"count\",\"value\":\"4\"},{\"key\":\"$version\",\"value\":\"0\"}]}\n", Description = "explicit version 0")]
        public void SavesWithoutVersionOneReadAsVersionZero(string raw)
        {
            var result = PreferencesFormat.Parse(raw);
            Assert.AreEqual(PreferencesLoadStatus.Loaded, result.Status, result.Error);
            Assert.AreEqual(1, result.Data.Count);
            Assert.AreEqual("4", result.Data["count"]);
        }

        [Test]
        public void FlatSaveKeepsValuesWithCommasAndEscapes()
        {
            var result = PreferencesFormat.Parse("{\"text\":\"a,b:c \\\"q\\\" \\u00e9\"}");
            Assert.AreEqual(PreferencesLoadStatus.Loaded, result.Status, result.Error);
            Assert.AreEqual("a,b:c \"q\" \u00e9", result.Data["text"]);
        }

        [Test]
        public void EmptyEntriesIsAValidEmptySave()
        {
            var result = PreferencesFormat.Parse("{\"_entries\":[]}");
            Assert.AreEqual(PreferencesLoadStatus.Loaded, result.Status, result.Error);
            Assert.AreEqual(0, result.Data.Count);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("null")]
        [TestCase("[]")]
        [TestCase("\"text\"")]
        [TestCase("42")]
        [TestCase("not json")]
        [TestCase("{")]
        [TestCase("{\"_entries\":[{\"key\":\"a\",\"value\":\"1\"}")]
        [TestCase("{\"_entries\":[{\"key\":\"a\",\"value\":\"1\"}]} trailing")]
        [TestCase("{\"_entries\":\"x\"}")]
        [TestCase("{\"_entries\":[{\"key\":\"a\"}]}")]
        [TestCase("{\"_entries\":[{\"key\":1,\"value\":\"x\"}]}")]
        [TestCase("{\"_entries\":[{\"key\":\"a\",\"value\":1}]}")]
        [TestCase("{\"_entries\":[\"a\"]}")]
        [TestCase("{\"a\":1}")]
        [TestCase("{\"a\":{\"b\":\"c\"}}")]
        [TestCase("{\"a\":\"unterminated}")]
        [TestCase("{\"_entries\":[{\"key\":\"\",\"value\":\"x\"}]}")]
        [TestCase("{\"_entries\":[{\"key\":\"$version\",\"value\":\"abc\"}]}")]
        public void DamagedSavesAreErrorsThatKeepTheOriginalText(string raw)
        {
            var result = PreferencesFormat.Parse(raw);
            Assert.AreEqual(PreferencesLoadStatus.Error, result.Status);
            Assert.IsNotEmpty(result.Error);
            Assert.AreEqual(raw, result.Raw);
            Assert.IsNull(result.Data);
        }

        [Test]
        public void ANewerVersionIsRefused()
        {
            var raw = "{\"_entries\":[{\"key\":\"count\",\"value\":\"4\"},{\"key\":\"$version\",\"value\":\"2\"}]}";
            var result = PreferencesFormat.Parse(raw);
            Assert.AreEqual(PreferencesLoadStatus.Error, result.Status);
            StringAssert.Contains("unknown version 2", result.Error);
            Assert.AreEqual(raw, result.Raw);
        }

        [Test]
        public void TheCurrentVersionIsAccepted()
        {
            var raw = "{\"_entries\":[{\"key\":\"$version\",\"value\":\"" + PreferencesFormat.CurrentVersion + "\"},{\"key\":\"count\",\"value\":\"4\"}]}";
            var result = PreferencesFormat.Parse(raw);
            Assert.AreEqual(PreferencesLoadStatus.Loaded, result.Status, result.Error);
            Assert.AreEqual("4", result.Data["count"]);
        }
    }
}
