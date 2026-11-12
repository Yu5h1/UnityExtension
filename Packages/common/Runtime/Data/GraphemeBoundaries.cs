
using System.Collections.Generic;
using System.Globalization;

namespace Yu5h1Lib
{
    /// <summary>Finds UTF-16 end offsets of text elements, including emoji sequences on Unity's Mono runtime.</summary>
    public static class GraphemeBoundaries
    {
        public static int[] GetEnds(string text)
        {
            var starts = StringInfo.ParseCombiningCharacters(text);
            var ends = new List<int>(starts.Length);
            int regionalRun = 0;
            for (int i = 1; i < starts.Length; i++)
            {
                int at = starts[i];
                for (int prior = starts[i - 1]; prior < at;)
                {
                    int priorCode = CodePointAt(text, prior);
                    regionalRun = IsRegional(priorCode) ? regionalRun + 1 : 0;
                    prior += priorCode > char.MaxValue ? 2 : 1;
                }
                int code = CodePointAt(text, at);
                bool paired = IsRegional(code) && regionalRun % 2 == 1;
                bool emojiJoin = text[at - 1] == '\u200d' &&
                    CharUnicodeInfo.GetUnicodeCategory(text, at) == UnicodeCategory.OtherSymbol;
                if (paired || emojiJoin || code == 0x200d ||
                    code >= 0x1f3fb && code <= 0x1f3ff ||
                    text[at - 1] == '\r' && code == '\n')
                    continue;
                ends.Add(at);
            }
            if (text.Length > 0)
                ends.Add(text.Length);
            return ends.ToArray();
        }

        private static int CodePointAt(string text, int index) =>
            char.IsSurrogatePair(text, index) ? char.ConvertToUtf32(text, index) : text[index];

        private static bool IsRegional(int code) => code >= 0x1f1e6 && code <= 0x1f1ff;
    }
}
