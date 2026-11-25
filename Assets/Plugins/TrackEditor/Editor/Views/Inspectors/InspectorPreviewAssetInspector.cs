using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrackEditor.Test;
using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    [CustomEditor(typeof(InspectorPreviewAsset))]
    public class InspectorPreviewAssetInspector : Editor
    {
        internal static event System.Action<InspectorPreviewAsset> SelectionEdited;

        private readonly Dictionary<IData, InspectorsBase> directableEditors = new Dictionary<IData, InspectorsBase>();
        private InspectorsBase currentDirectableEditor;

        protected override void OnHeaderGUI()
        {
            GUILayout.Space(18f);
        }

        public override void OnInspectorGUI()
        {
            var preview = (InspectorPreviewAsset)target;
            var selection = preview.SelectedObject;
            if (selection == null)
            {
                currentDirectableEditor = null;
                EditorGUILayout.HelpBox(Lan.NotSelectAsset, MessageType.Info);
                return;
            }

            GUI.skin.GetStyle("label").richText = true;
            GUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            DoSelectionInspector(selection);
            if (EditorGUI.EndChangeCheck())
            {
                SelectionEdited?.Invoke(preview);
            }
        }

        void DoSelectionInspector(DirectableAsset selection)
        {
            if (!(selection is IData data))
                return;

            if (!directableEditors.TryGetValue(data, out var newEditor))
            {
                directableEditors[data] = newEditor = EditorInspectorFactory.GetInspector(data);
            }

            if (currentDirectableEditor != newEditor)
            {
                var enableMethod = newEditor
                    .GetType()
                    .GetMethod(
                        "OnEnable",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy
                    );
                if (enableMethod != null)
                {
                    enableMethod.Invoke(newEditor, null);
                }

                currentDirectableEditor = newEditor;
            }

            EditorTools.BoldSeparator();
            GUILayout.Space(4);
            ShowPreliminaryInspector(selection);

            if (currentDirectableEditor != null)
                currentDirectableEditor.OnInspectorGUI();
        }

        /// <summary>
        /// 选中对象基本信息
        /// </summary>
        void ShowPreliminaryInspector(DirectableAsset selection)
        {
            var type = selection.GetType();
            var nameAtt = type.GetCustomAttributes(typeof(NameAttribute), false).FirstOrDefault() as NameAttribute;
            var name = nameAtt != null ? nameAtt.name : type.Name.SplitCamelCase();

            GUI.color = new Color(0, 0, 0, 0.2f);
            GUILayout.BeginHorizontal(Styles.headerBoxStyle);
            GUI.color = Color.white;

            GUILayout.Label($"<b><size=18>{name}</size></b>");

            GUILayout.EndHorizontal();

            var desAtt = type.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            var description = desAtt != null ? desAtt.description : string.Empty;
            if (!string.IsNullOrEmpty(description))
            {
                EditorGUILayout.HelpBox(description, MessageType.None);
            }

            GUILayout.Space(2);
        }
    }
}
