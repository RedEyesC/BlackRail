using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    [CustomInspectors(typeof(Asset), true)]
    public class AssetInspector : InspectorsBase
    {
        private Asset action => (Asset)target;

        public override void OnInspectorGUI()
        {
            ShowCommonInspector();
            base.OnInspectorGUI();
        }

        protected void ShowCommonInspector(bool showBaseInspector = true)
        {
            var label = new GUIContent(Prefs.DisplayFrames ? "总长度（帧）" : "总长度（秒）");
            EditorGUI.BeginChangeCheck();
            var length = EditorTools.DelayedTimeField(label, action.Length);
            if (EditorGUI.EndChangeCheck() && length != action.Length)
            {
                action.Length = length;
            }
        }
    }
}
