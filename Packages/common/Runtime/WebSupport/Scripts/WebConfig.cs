using System.Runtime.InteropServices;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib.WebSupport
{
    public class WebConfig
    {
        private static DataView _Properties;
        public static DataView Properties => _Properties;


        public static bool TryGetValue(string key, out string value,bool allowEmpty = true)
        {
            value = null;
            if (!TryLoad())
                return false;
            if (_Properties == null)
                return false;

            if (Properties.TryGetValue(key, out value))
            {
                if (allowEmpty)
                    return true;
                return !value.IsEmpty();
            }
            else
                $"WebConfig key:{key} not found ! ".printWarning();
            return false;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string GetConfig();
#endif
        public static bool TryLoad()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return DataView.TryParseFromJson(GetConfig(), out _Properties);
            "WebConfig not found".printWarning();
#endif
            return false;
        }
    }
}