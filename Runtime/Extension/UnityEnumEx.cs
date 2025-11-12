using System.ComponentModel;
using System.Linq;

using Enum = System.Enum;
using Yu5h1Lib;

namespace Yu5h1Lib
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class UnityEnumEx
    {
        public static T RandomElement<T>(this T current, bool ignoreCurrent) where T : Enum
            => (ignoreCurrent ? current.GetValuesExcluding() : Enum.GetValues(typeof(T)).Cast<T>()).RandomElement();
    }
}
