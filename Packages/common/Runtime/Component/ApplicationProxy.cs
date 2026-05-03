using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Proxy component that exposes <see cref="UnityEngine.Application"/> static methods
    /// as instance methods for use with UnityEvent and Inspector (e.g. Button.OnClick).
    /// </summary>
    [AddComponentMenu("Yu5h1Lib/Application Proxy")]
    public class ApplicationProxy : MonoBehaviour
    {
        /// <summary>
        /// Quits the application. In Editor Play Mode, stops Play instead.
        /// </summary>
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Opens the specified URL in the default browser.
        /// </summary>
        public void OpenURL(string url) => Application.OpenURL(url);
    }
}
