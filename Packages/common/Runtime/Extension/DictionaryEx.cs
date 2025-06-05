using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Yu5h1Lib
{

    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class DictionaryEx
    {
        public static bool TryFind<T,U>(this Dictionary<T,U> dictionary, System.Func<KeyValuePair<T, U>, bool> predicate,
            out KeyValuePair<T, U> result)

        {
            result = default(KeyValuePair<T, U>);
            foreach (var kvp in dictionary)
                if (predicate(kvp))
                {
                    result = kvp;
                    return true;
                }
            return false;
        } 

    }
}
