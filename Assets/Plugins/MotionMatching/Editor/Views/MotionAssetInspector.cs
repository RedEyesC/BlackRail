using TrackEditor;
using UnityEditor;
using UnityEditor.Callbacks;

namespace MotionMatching
{
    internal static class MotionAssetInspector
    {
        [MenuItem("Assets/Create/MotionAsset", false, 30)]
        private static void CreateAsset() => AssetStorage.Create<MotionAsset>("New MotionAsset");

        [MenuItem("Window/Motion Editor")]
        private static void OpenWindow() => MotionEditorWindow.ShowWindow<MotionEditorWindow>();

        [OnOpenAsset]
        private static bool OpenTimeline(int instanceId, int line) =>
            AssetStorage.TryOpen<MotionAsset, MotionEditorWindow>(EditorUtility.InstanceIDToObject(instanceId));
    }
}
