using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yu5h1Lib.Utility
{
    public static class SceneUtility
    {
        private static GameObject _dontDestroyOnLoadAgent;
        public static GameObject DontDestroyOnLoadAgent 
        { 
            get
            {
                if (_dontDestroyOnLoadAgent == null )
                {
                    if ("DontDestroyOnLoadAgent only allow in play mode".printWarningIf(!Application.isPlaying))
                        return null;
                    _dontDestroyOnLoadAgent = new GameObject(nameof(DontDestroyOnLoadAgent));
                    GameObject.DontDestroyOnLoad(_dontDestroyOnLoadAgent);
                }
                return _dontDestroyOnLoadAgent;
            }
        }
        public static Scene[] GetLoadedScenes()
        {
            Scene[] results = new Scene[SceneManager.loadedSceneCount];
            for (int i = 0; i < results.Length; i++)
                results[i] = SceneManager.GetSceneAt(i);
            return results;
        }
        public static bool GetSceneOfDontDestroyOnLoad(out Scene SceneOfDontDestroyOnLoad)
        {
            SceneOfDontDestroyOnLoad = default;
            if (DontDestroyOnLoadAgent)
                SceneOfDontDestroyOnLoad = _dontDestroyOnLoadAgent.scene;
            return _dontDestroyOnLoadAgent;
        }
             
        public static void DeleteAgent() => GameObject.DestroyImmediate(_dontDestroyOnLoadAgent);
        public static bool DoesAgentExists() => _dontDestroyOnLoadAgent;
    }
}
