using UnityEditor;
using UnityEngine;

namespace MotionMatching
{
    [CustomEditor(typeof(MotionAsset))]
    internal class MotionAssetInspector : Editor
    {
        MotionAsset _asset;

        void OnEnable()
        {
            _asset = target as MotionAsset;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(20);

            if (GUILayout.Button("Open MotionAction Editor"))
            {
                if (target != null)
                {
                    Selection.activeObject = target as UnityEngine.Object;
                }

                MotionEditorWindow.OpenDirectorWindow();
            }
        }
    }
}
