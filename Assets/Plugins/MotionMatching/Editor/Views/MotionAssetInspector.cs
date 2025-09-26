using UnityEditor;
using UnityEngine;

namespace MotionMatching
{
    [CustomEditor(typeof(MotionAsset))]
    internal class MotionAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(20);

            if (GUILayout.Button("Open MotionAction Editor"))
            {
                if (target != null)
                {
                    Selection.activeObject = target;
                }

                MotionEditorWindow.OpenDirectorWindow();
            }
        }
    }
}
