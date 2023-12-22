using UnityEditor;
using UnityEditor.Callbacks;

namespace TasukeChan
{
    public class TasukeAssetHandler
    {
        [OnOpenAsset]
        public static bool OpenAsset(int instanceID, int line)
        {
            string path = AssetDatabase.GetAssetPath(instanceID);
            UnityEngine.Object obj = EditorUtility.InstanceIDToObject(instanceID);

            if (obj is TkData)
            {
                TasukeBoard.Load(path,true);
                return true;
            }

            return false;
        }
    }
}