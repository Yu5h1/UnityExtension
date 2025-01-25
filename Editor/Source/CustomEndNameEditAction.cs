using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;

namespace Yu5h1Lib.EditorExtension
{
    public class CustomEndNameEditAction : EndNameEditAction
    {
        public static System.Func<string, string, string> ReplaceContentAction = null;
        public override void Action(int instanceId, string path, string contents)
        {            
            string finalFileName = Path.GetFileNameWithoutExtension(path);
            if (ReplaceContentAction != null) contents = ReplaceContentAction(finalFileName, contents);
            else contents = contents.Replace("#SCRIPTNAME#", finalFileName);
            
            File.WriteAllText(path, contents);
            AssetDatabase.ImportAsset(path);
            ProjectWindowUtil.ShowCreatedAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
            CustomEndNameEditAction.ReplaceContentAction = null;
        }
    }
}