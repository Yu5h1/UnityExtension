using System.ComponentModel;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.Common
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class ObjectEx
    {
        public const string MENU_PATH = "CONTEXT/Object/";
        public const string CopyReference_PATH = MENU_PATH + "Copy Reference";

        [MenuItem(CopyReference_PATH)]
        private static void CopyReference(MenuCommand command)
        {
            Object obj = command.context;
            if (obj == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long localId))
                return;

            var wrapper = new ObjectWrapperJSON
            {
                guid = guid,
                localId = localId,
                type = 2,
                instanceID = obj.GetInstanceID()
            };
            EditorGUIUtility.systemCopyBuffer = "UnityEditor.ObjectWrapperJSON:" + JsonUtility.ToJson(wrapper);
        }

        [System.Serializable]
        private class ObjectWrapperJSON
        {
            public string guid;
            public long localId;
            public int type;
            public int instanceID;
        }
    }
}
