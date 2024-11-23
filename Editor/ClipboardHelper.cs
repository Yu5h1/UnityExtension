using Type = System.Type;
using System.IO;
using UnityEngine;
using UnityEditor;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using CallbackFunction = UnityEditor.EditorApplication.CallbackFunction;
using System.Reflection;
using System.Threading;

namespace Yu5h1Lib.EditorExtension
{
    public static class ClipboardHelper
    {
        static string AssemblyLocation => AssetDatabaseEx.AssemblyLocation;
        static string ClipboardHelperPath = AssemblyLocation + @"\ClipboardHelper.exe";
        static bool IsClipboardHelperExist => File.Exists(ClipboardHelperPath);

        static FieldInfo globalEventHandlerFieldInfo = typeof(EditorApplication).GetField("globalEventHandler", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        public static CallbackFunction globalEventHandler
        {
            get { return (CallbackFunction)globalEventHandlerFieldInfo.GetValue(null); }
            set
            {
                CallbackFunction functions = (CallbackFunction)globalEventHandlerFieldInfo.GetValue(null);
                functions += value;
                globalEventHandlerFieldInfo.SetValue(null, (object)functions);
            }
        }

        static Type ProjectBrowserType { get { return ProjectBrowserEx.InternalType; } }
        public static bool IsFocuseProjectWindow {
            get {
                if (EditorWindow.focusedWindow == null) return false;
                return EditorWindow.focusedWindow.GetType() == ProjectBrowserType;
            }
        }        
        public static string CorrectAssetsPath(string path) { return path.Replace("/", "\\"); }

        static void FileToClipBoard(bool CutOrCopy)
        {
            if (IsClipboardHelperExist == false || IsFocuseProjectWindow == false) return;
            var selObjs = Selection.objects;
            if (selObjs.Length > 0)
            {                
                string copypaths = "";
                foreach (var item in selObjs)
                {
                    if (AssetDatabase.Contains(item))
                    {
                        string fullpath = CorrectAssetsPath(Path.GetFullPath(AssetDatabase.GetAssetPath(item)));
                        if (fullpath != CorrectAssetsPath(Application.dataPath))
                            copypaths += fullpath + System.Environment.NewLine;
                    }
                }
                CallClipboardHelper(
                    null!,
                    CutOrCopy ? "cut" : "copy",
                    copypaths
                );
                EditorGUIUtility.PingObject(selObjs[0]);
            }
            
        }
        [MenuItem("Assets/Copy Assets", true)]
        public static bool CopyAssetsToClipbordValidation()
        {
            if (IsClipboardHelperExist == false) return false;
            if (IsFocuseProjectWindow == false) return false;
            return Selection.objects.Length > 0;
        }
        [MenuItem("Assets/Copy Assets")]
        public static void CopyAssetsToClipboard() { FileToClipBoard(false); }
        [MenuItem("Assets/Paste Assets", true)]
        public static bool PasteAssetsFromClipbordValidation() {

            if (IsClipboardHelperExist == false) return false;

            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            var p = CallClipboardHelper
            (
                 () => { ewh.Set(); },
                 "IsPasteAvailable"
            );
            ewh.WaitOne();
            return p.ExitCode == 1;
        }
        [MenuItem("Assets/Paste Assets")]
        public static void PasteAssetsFromClipbord()
        {
            if ("ClipboardHelper Does notExists !".printWarningIf(!IsClipboardHelperExist))
                return;
            CallClipboardHelper
            (
                () => { EditorApplication.update += AssetDatabaseRefreshInUnityThread; },
                "paste", ProjectBrowserEx.SelectedFolderPath, ".meta"
            );
        }

        static Process CallClipboardHelper(System.Action Exited,params string[] args) {
            if ("ClipboardHelper Does notExists !".printWarningIf(!IsClipboardHelperExist))
                return null!;

            string arguments = "";
            if (args != null) {
                foreach (var item in args) arguments += "\""+item + "\" ";
            }
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(ClipboardHelperPath, arguments);
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            if (Exited != null) {
                p.EnableRaisingEvents = true;
                p.Exited += delegate (object sender, System.EventArgs e) { Exited(); };
            }
            p.Start();
            return p;
        }
        static void AssetDatabaseRefreshInUnityThread()
        {
            AssetDatabase.Refresh();
            EditorApplication.update -= AssetDatabaseRefreshInUnityThread;
        }
        [InitializeOnLoadMethod]
        public static void CopyPasteKeyboardShortcut()
        {
            if (IsClipboardHelperExist == false) return;
            globalEventHandler += () =>
            {
                if (IsFocuseProjectWindow)
                {
                    var e = Event.current;
                    if (EditorGUIUtility.editingTextField == false)
                    {
                        if (e.type == EventType.KeyUp)
                        {
                            if (e.keyCode == KeyCode.C)
                            {
                                CopyAssetsToClipboard();
                            }
                            if (e.keyCode == KeyCode.X)
                            {
                                FileToClipBoard(true);
                            }
                            if (e.keyCode == KeyCode.V)
                            {
                                PasteAssetsFromClipbord();
                            }
                        }
                    }
                }
            };
        }
    }
}
