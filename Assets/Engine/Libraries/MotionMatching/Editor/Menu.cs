
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Editor
{
    public class Menu
    {

        [MenuItem("Assets/Create/MotionMatching Asset", false, 30)]
        private static void CreateAsset()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateKinematicaAsset>(), "New Kinematica Asset.asset", null, null);
        }

        internal class DoCreateKinematicaAsset : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var asset = ScriptableObject.CreateInstance<Asset>();
                AssetDatabase.CreateAsset(asset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(asset);
            }
        }

    }
}