using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib
{
    //[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public class EditorPreferences : SettingsProvider
    {
        public static Dictionary<string, System.Action> _drawMethods;
        public static Dictionary<string, System.Action> drawMethods
        {
            get
            {
                if (_drawMethods == null)
                    _drawMethods = new Dictionary<string, System.Action>();
                return _drawMethods;
            }
        }
        public static void AddDrawer(string key,System.Action action)
        {
            if (drawMethods.ContainsKey(key))
                drawMethods[key] += action;
            else
                drawMethods[key] = action;
        }

        private static int selectedTab = 0;
        private static string[] tabNames = new string[0];

        public EditorPreferences(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) { }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new EditorPreferences("Preferences/Yu5h1Lib Settings", SettingsScope.User);
            provider.keywords = GetSearchKeywordsFromGUIContentProperties<GUIContent>();
            return provider;
        }

        public override void OnGUI(string searchContext)
        {

            if (drawMethods.IsEmpty() && tabNames.IsEmpty()) return;
            
            tabNames = drawMethods.Keys.Select(k => k).ToArray();

            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            GUILayout.Space(10);

            if (selectedTab < drawMethods.Count)
                drawMethods[tabNames[selectedTab]]?.Invoke();
        }
    }
}
