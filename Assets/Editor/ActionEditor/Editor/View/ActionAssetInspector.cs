using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    [CustomEditor(typeof(ActionImporter))]
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
                    Selection.activeObject = target as UnityEngine.Object;
                }

                ActionEditorWindow.OpenDirectorWindow();
            }
        }
    }
}
