using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.EditorExtension
{
    public static class EditorGUIEx
    {
        public static string SearchBar(string SearchText)
        {
            EditorGUILayout.BeginHorizontal();
            SearchText = EditorGUILayout.TextField(SearchText, new GUIStyle("SearchTextField"));
            string SearchCancelButtonName = SearchText == "" ? "SearchCancelButtonEmpty" : "SearchCancelButton";
            if (GUILayout.Button("", new GUIStyle(SearchCancelButtonName)))
            {
                SearchText = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            return SearchText;
        }
        public static Vector2 SearchableMenu<T>(T[] SearchList, ref string SearchText, Vector2 scrollPos, System.Action<int> ElementAction,
                                                int scrollViewHeight = 150, int elementHeight = 25)
        {
            SearchText = SearchBar(SearchText);
            int count = scrollViewHeight / elementHeight;
            if (SearchList.Length < count) scrollViewHeight = SearchList.Length * elementHeight;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollViewHeight));
            for (int i = 0; i < SearchList.Length; i++)
            {
                string curName = SearchList[i].ToString();
                if (StringUtil.MatchableText(SearchText, curName))
                {
                    if (GUILayout.Button(curName))
                    {
                        ElementAction(i);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            return scrollPos;
        }
        public static Vector2 SearchableMenu<T>(List<T> SearchList, ref string SearchText, Vector2 scrollPos, System.Action<int> ElementAction,
                                            int scrollViewHeight = 150, int elementHeight = 25)
        { return SearchableMenu(SearchList.ToArray(), ref SearchText, scrollPos, index => ElementAction(index), scrollViewHeight, elementHeight); }

        public static Vector2 SearchableMenu<TKey, TValue>(bool UseKeyAsName, Dictionary<TKey, TValue> SearchDictionary, ref string SearchText, Vector2 scrollPos, System.Action<TKey> ElementAction,
                                            int scrollViewHeight = 150, int elementHeight = 25)
        {
            SearchText = SearchBar(SearchText);
            int count = scrollViewHeight / elementHeight;
            if (SearchDictionary.Count < count) scrollViewHeight = SearchDictionary.Count * elementHeight;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollViewHeight));

            foreach (var item in SearchDictionary)
            {
                string curName = UseKeyAsName ? item.Key.ToString() : item.Value.ToString();
                if (StringUtil.MatchableText(SearchText, curName))
                {
                    if (GUILayout.Button(curName))
                    {
                        ElementAction(item.Key);
                        break;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            return scrollPos;
        }
    } 
}
