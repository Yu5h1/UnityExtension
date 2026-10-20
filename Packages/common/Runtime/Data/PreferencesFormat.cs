using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public enum PreferencesLoadStatus
    {
        /// <summary>The storage holds no save for the KEY.</summary>
        Missing,
        /// <summary>A save was read and understood.</summary>
        Loaded,
        /// <summary>A save exists but cannot be read (damaged, unknown version, read failure).</summary>
        Error
    }

    /// <summary>What reading one KEY from storage produced. <see cref="Raw"/> keeps the original text of a save that
    /// could not be understood, so nothing needs to overwrite it to report on it.</summary>
    public readonly struct PreferencesLoadResult
    {
        public readonly PreferencesLoadStatus Status;
        public readonly DataView Data;
        public readonly string Raw;
        public readonly string Error;

        private PreferencesLoadResult(PreferencesLoadStatus status, DataView data, string raw, string error)
        {
            Status = status;
            Data = data;
            Raw = raw;
            Error = error;
        }

        public static PreferencesLoadResult Missing => new PreferencesLoadResult(PreferencesLoadStatus.Missing, null, null, null);
        public static PreferencesLoadResult Loaded(DataView data) => new PreferencesLoadResult(PreferencesLoadStatus.Loaded, data, null, null);
        public static PreferencesLoadResult Failed(string raw, string error) => new PreferencesLoadResult(PreferencesLoadStatus.Error, null, raw, error);
    }

    /// <summary>
    /// The save format of one KEY (UnityExtension decision <c>config-storage-format</c>): the DataView as
    /// <c>{"_entries":[{"key":"a","value":"1"}, ...]}</c> plus a reserved <see cref="VersionField"/> entry. A save
    /// without the version entry (older entries format, or the even older flat <c>{"a":"1"}</c> object) is version 0
    /// and reads normally; a higher version than <see cref="CurrentVersion"/> is refused. Anything that is not
    /// valid JSON of one of these shapes is damaged. The parser is strict on purpose: a lenient guess would let the
    /// next write replace a save that only looked unreadable.
    /// </summary>
    public static class PreferencesFormat
    {
        /// <summary>Reserved entry holding the format version; never visible in a loaded view.</summary>
        public const string VersionField = "$version";
        public const int CurrentVersion = 1;

        public static PreferencesLoadResult Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return PreferencesLoadResult.Failed(raw, "the save is empty");
            if (!JsonReader.TryRead(raw, out var root, out var error))
                return PreferencesLoadResult.Failed(raw, "not valid JSON: " + error);
            if (!(root is Dictionary<string, object> obj))
                return PreferencesLoadResult.Failed(raw, "the top-level value is not an object");

            var pairs = new List<KeyValuePair<string, string>>();
            if (obj.TryGetValue("_entries", out var entries))
            {
                if (!(entries is List<object> list))
                    return PreferencesLoadResult.Failed(raw, "'_entries' is not an array");
                for (int i = 0; i < list.Count; i++)
                {
                    if (!(list[i] is Dictionary<string, object> item)
                        || !item.TryGetValue("key", out var key) || !(key is string k)
                        || !item.TryGetValue("value", out var value) || !(value is string v))
                        return PreferencesLoadResult.Failed(raw, $"entry {i} is not a string key/value pair");
                    pairs.Add(new KeyValuePair<string, string>(k, v));
                }
            }
            else
            {
                foreach (var pair in obj)
                {
                    if (!(pair.Value is string s))
                        return PreferencesLoadResult.Failed(raw, $"field '{pair.Key}' is not a string");
                    pairs.Add(new KeyValuePair<string, string>(pair.Key, s));
                }
            }

            var data = new DataView();
            int version = 0;
            foreach (var pair in pairs)
            {
                if (string.Equals(pair.Key, VersionField, System.StringComparison.Ordinal))
                {
                    if (!int.TryParse(pair.Value, NumberStyles.None, CultureInfo.InvariantCulture, out version))
                        return PreferencesLoadResult.Failed(raw, $"'{VersionField}' is not a version number");
                    continue;
                }
                if (string.IsNullOrEmpty(pair.Key))
                    return PreferencesLoadResult.Failed(raw, "an entry has an empty key");
                data[pair.Key] = pair.Value;
            }
            if (version > CurrentVersion)
                return PreferencesLoadResult.Failed(raw, $"unknown version {version} (this build reads up to {CurrentVersion})");
            return PreferencesLoadResult.Loaded(data);
        }

        /// <summary>The JSON a save of <paramref name="data"/> writes: its entries plus the version entry.</summary>
        public static string ToJson(DataView data)
        {
            var copy = new DataView();
            foreach (var pair in data)
                if (!string.Equals(pair.Key, VersionField, System.StringComparison.Ordinal))
                    copy[pair.Key] = pair.Value;
            copy[VersionField] = CurrentVersion.ToString(CultureInfo.InvariantCulture);
            return copy.ToJson();
        }

        /// <summary>A small strict JSON reader: objects become <c>Dictionary&lt;string, object&gt;</c>, arrays
        /// <c>List&lt;object&gt;</c>, strings <c>string</c>, numbers <c>double</c>, booleans <c>bool</c>, null null.
        /// Content after the root value is an error.</summary>
        private sealed class JsonReader
        {
            private const int MaxDepth = 64;
            private readonly string s;
            private int i;

            private JsonReader(string text) => s = text;

            private sealed class ReadException : System.Exception
            {
                public ReadException(string message) : base(message) { }
            }

            public static bool TryRead(string text, out object value, out string error)
            {
                var reader = new JsonReader(text);
                try
                {
                    reader.SkipWhitespace();
                    value = reader.ReadValue(0);
                    reader.SkipWhitespace();
                    if (reader.i != text.Length)
                        throw new ReadException($"unexpected content at position {reader.i}");
                    error = null;
                    return true;
                }
                catch (ReadException e)
                {
                    value = null;
                    error = e.Message;
                    return false;
                }
            }

            private void SkipWhitespace()
            {
                while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n'))
                    i++;
            }

            private object ReadValue(int depth)
            {
                if (depth > MaxDepth)
                    throw new ReadException("nesting is too deep");
                if (i >= s.Length)
                    throw new ReadException("unexpected end of text");
                switch (s[i])
                {
                    case '{': return ReadObject(depth);
                    case '[': return ReadArray(depth);
                    case '"': return ReadString();
                    case 't': ReadLiteral("true"); return true;
                    case 'f': ReadLiteral("false"); return false;
                    case 'n': ReadLiteral("null"); return null;
                    default: return ReadNumber();
                }
            }

            private void ReadLiteral(string literal)
            {
                if (string.CompareOrdinal(s, i, literal, 0, literal.Length) != 0)
                    throw new ReadException($"unexpected token at position {i}");
                i += literal.Length;
            }

            private object ReadNumber()
            {
                int start = i;
                while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0)
                    i++;
                if (i == start || !double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    throw new ReadException($"unexpected token at position {start}");
                return number;
            }

            private Dictionary<string, object> ReadObject(int depth)
            {
                var result = new Dictionary<string, object>();
                i++;
                SkipWhitespace();
                if (i < s.Length && s[i] == '}') { i++; return result; }
                while (true)
                {
                    SkipWhitespace();
                    if (i >= s.Length || s[i] != '"')
                        throw new ReadException($"expected a string key at position {i}");
                    var key = ReadString();
                    SkipWhitespace();
                    if (i >= s.Length || s[i] != ':')
                        throw new ReadException($"expected ':' at position {i}");
                    i++;
                    SkipWhitespace();
                    result[key] = ReadValue(depth + 1);
                    SkipWhitespace();
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    if (i < s.Length && s[i] == '}') { i++; return result; }
                    throw new ReadException($"expected ',' or '}}' at position {i}");
                }
            }

            private List<object> ReadArray(int depth)
            {
                var result = new List<object>();
                i++;
                SkipWhitespace();
                if (i < s.Length && s[i] == ']') { i++; return result; }
                while (true)
                {
                    SkipWhitespace();
                    result.Add(ReadValue(depth + 1));
                    SkipWhitespace();
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    if (i < s.Length && s[i] == ']') { i++; return result; }
                    throw new ReadException($"expected ',' or ']' at position {i}");
                }
            }

            private string ReadString()
            {
                var sb = new StringBuilder();
                i++;
                while (true)
                {
                    if (i >= s.Length)
                        throw new ReadException("unterminated string");
                    char c = s[i++];
                    if (c == '"')
                        return sb.ToString();
                    if (c < ' ')
                        throw new ReadException($"control character in string at position {i - 1}");
                    if (c != '\\') { sb.Append(c); continue; }
                    if (i >= s.Length)
                        throw new ReadException("unterminated escape");
                    char e = s[i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 > s.Length || !int.TryParse(s.Substring(i, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code))
                                throw new ReadException($"bad \\u escape at position {i}");
                            sb.Append((char)code);
                            i += 4;
                            break;
                        default:
                            throw new ReadException($"bad escape '\\{e}' at position {i - 1}");
                    }
                }
            }
        }
    }
}
