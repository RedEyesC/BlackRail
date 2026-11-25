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

        private DirectableAsset currentSelection;
        private InspectorsBase currentDirectableEditor;

        private void OnEnable()
        {
            if (target is InspectorPreviewAsset preview)
            {
                preview.RepaintRequested += Repaint;
            }
        }

        private void OnDisable()
        {
            if (target is InspectorPreviewAsset preview)
            {
                preview.RepaintRequested -= Repaint;
            }
        }

        protected override void OnHeaderGUI()
        {
            if (!(target is InspectorPreviewAsset preview) || preview == null)
            {
                return;
            }

            var selection = preview.SelectedObject;
            if (selection == null)
            {
                return;
            }

            var type = selection.GetType();
            var typeName = type.GetCustomAttribute<NameAttribute>(false)?.name
                ?? ObjectNames.NicifyVariableName(type.Name);

            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var icon = EditorGUIUtility.ObjectContent(null, GetHeaderIconType(selection)).image;
                var iconRect = GUILayoutUtility.GetRect(28f, 28f, GUILayout.ExpandWidth(false));
                if (icon != null)
                {
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    if (selection is Group || selection is Track || selection is Clip)
                    {
                        var name = selection is Clip clip ? clip.DisplayName : selection.name;
                        var editedName = EditorGUILayout.TextField(name);
                        if (editedName != name)
                        {
                            selection.name = editedName;
                        }

                        EditorGUILayout.LabelField(typeName, EditorStyles.miniLabel);
                    }
                    else if (selection is Asset)
                    {
                        EditorGUILayout.LabelField(selection.name, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(typeName, EditorStyles.miniLabel);
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                SelectionEdited?.Invoke(preview);
            }
        }

        private static System.Type GetHeaderIconType(DirectableAsset selection)
        {
            switch (selection)
            {
                case Group _: return typeof(GameObject);
                case Track _: return typeof(Animator);
                case Clip _: return typeof(AnimationClip);
                default: return typeof(TextAsset);
            }
        }

        public override void OnInspectorGUI()
        {
            if (!(target is InspectorPreviewAsset preview) || preview == null)
            {
                return;
            }

            var selection = preview.SelectedObject;
            if (selection == null)
            {
                currentSelection = null;
                currentDirectableEditor = null;
                EditorGUILayout.HelpBox(Lan.NotSelectAsset, MessageType.Info);
                return;
            }

            if (!ReferenceEquals(currentSelection, selection))
            {
                currentDirectableEditor = CreateInspector(selection);
                currentSelection = selection;
            }

            EditorGUI.BeginChangeCheck();
            DrawInspector(selection, currentDirectableEditor);
            if (EditorGUI.EndChangeCheck())
            {
                SelectionEdited?.Invoke(preview);
            }
        }

        private static InspectorsBase CreateInspector(DirectableAsset selection)
        {
            var inspector = EditorInspectorFactory.GetInspector(selection);
            var enableMethod = inspector.GetType().GetMethod(
                "OnEnable",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            enableMethod?.Invoke(inspector, null);
            return inspector;
        }

        private static void DrawInspector(DirectableAsset selection, InspectorsBase inspector)
        {
            var description = selection.GetType().GetCustomAttribute<DescriptionAttribute>(false)?.description;
            if (!string.IsNullOrEmpty(description))
            {
                EditorGUILayout.HelpBox(description, MessageType.None);
            }

            inspector.OnInspectorGUI();
        }
    }
}
