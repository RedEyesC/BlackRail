using TrackEditor;
using UnityEditor;
using UnityEngine;

namespace GameEditor.ActionEditor
{
    public class ActionEditorWindow : TrackEditorWindow
    {
        public static bool OpenAsset(TextAsset source) => AssetStorage.TryOpen<ActionAsset, ActionEditorWindow>(source);

        public static bool OpenAsset(string path) => OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(path));

        public static TextAsset CreateAsset(string path) => AssetStorage.CreateAtPath<ActionAsset>(path);

        protected override System.Type AssetType => typeof(ActionAsset);

        protected override GameObject FindPreviewTarget() => GameObject.Find("Player");

        protected override AnimationClip ResolvePreviewAnimation(string path) =>
            base.ResolvePreviewAnimation(path) ?? AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resource/Anim/" + path + ".anim");

        protected override string CreateWindowTitle()
        {
            return "Action Editor";
        }
    }
}
