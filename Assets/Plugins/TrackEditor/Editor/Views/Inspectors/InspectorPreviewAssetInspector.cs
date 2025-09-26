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
        private bool _optionsAssetFold = true;

        private static Asset _lastAsset;
        private static bool _willResample;

        private static Dictionary<IData, InspectorsBase> directableEditors = new Dictionary<IData, InspectorsBase>();

        private static InspectorsBase _currentDirectableEditor;
        private static InspectorsBase _currentAssetEditor;

        void OnEnable()
        {
            _currentDirectableEditor = null;
            _willResample = false;
        }

        void OnDisable()
        {
            _currentDirectableEditor = null;
            directableEditors.Clear();
            _willResample = false;
        }

        protected override void OnHeaderGUI()
        {
            GUILayout.Space(18f);
        }

        public override void OnInspectorGUI()
        {
            var ow = target as InspectorPreviewAsset;
            if (ow == null || DirectorUtility.selectedObject == null)
            {
                EditorGUILayout.HelpBox(Lan.NotSelectAsset, MessageType.Info);
                return;
            }

            GUI.skin.GetStyle("label").richText = true;

            GUILayout.Space(5);

            DoAssetInspector();
            DoSelectionInspector();

            if (_willResample)
            {
                _willResample = false;
                EditorApplication.delayCall += () =>
                {
                    Debug.Log("cutscene.ReSample();");
                };
            }

            Repaint();
        }

        void DoAssetInspector()
        {
            //
        }

        void DoSelectionInspector()
        {
            var selection = DirectorUtility.selectedObject;

            if (selection == null)
            {
                _currentDirectableEditor = null;
                return;
            }

            if (!(selection is IData data))
                return;

            if (!directableEditors.TryGetValue(data, out var newEditor))
            {
                directableEditors[data] = newEditor = EditorInspectorFactory.GetInspector(data);
            }

            if (_currentDirectableEditor != newEditor)
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

                _currentDirectableEditor = newEditor;
            }

            EditorTools.BoldSeparator();
            GUILayout.Space(4);
            ShowPreliminaryInspector();

            if (_currentDirectableEditor != null)
                _currentDirectableEditor.OnInspectorGUI();
        }

        /// <summary>
        /// 选中对象基本信息
        /// </summary>
        void ShowPreliminaryInspector()
        {
            var type = DirectorUtility.selectedObject.GetType();
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
