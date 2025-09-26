using TrackEditor;
using UnityEditor;
using UnityEngine;

namespace MotionMatching
{
    public class MotionAsset : Asset
    {
        internal class DoCreateMotionAsset : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var asset = ScriptableObject.CreateInstance<MotionAsset>();
                AssetDatabase.CreateAsset(asset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(asset);
            }
        }

        [MenuItem("Assets/Create/MotionAsset", false, 30)]
        private static void CreateAsset()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreateMotionAsset>(),
                "New MotionAssets.asset",
                null,
                null
            );
        }
    }
}
