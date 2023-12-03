using UnityEngine;

namespace TasukeChan
{

    [UnityEditor.AssetImporters.ScriptedImporter(1, "tk")]
    public class TasukeImporter : UnityEditor.AssetImporters.ScriptedImporter
    {
        public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
        {
            var nodeWrapper = ScriptableObject.CreateInstance<TkData>();
            ctx.AddObjectToAsset("main", nodeWrapper);
            ctx.SetMainObject(nodeWrapper);
        }
    }
}