using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    [CustomEditor(typeof(ActionAssetImporter))]
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
    }
}
