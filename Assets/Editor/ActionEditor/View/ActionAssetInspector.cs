using MotionMatching;
using UnityEditor;
using UnityEngine;

namespace GameEditor.ActionEditor
{
    [CustomEditor(typeof(ActionAsset))]
    internal class ActionAssetInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(20);

            if (GUILayout.Button("Open Action Editor"))
            {
                if (target != null)
                {
                    var path = AssetDatabase.GetAssetPath(target);
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    Selection.activeObject = asset != null ? asset : target as UnityEngine.Object;
                }

                ActionEditorWindow.ShowWindow<ActionEditorWindow>();
            }
        }

        internal class DoCreateActionAsset : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var asset = ScriptableObject.CreateInstance<ActionAsset>();
                AssetDatabase.CreateAsset(asset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(asset);
            }
        }

        [MenuItem("Assets/Create/ActionAsset", false, 30)]
        private static void CreateAsset()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreateActionAsset>(),
                "New ActionAssets.asset",
                null,
                null
            );
        }
    }
}
