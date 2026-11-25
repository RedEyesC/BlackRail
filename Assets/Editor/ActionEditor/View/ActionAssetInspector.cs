using System.Runtime.CompilerServices;
using TrackEditor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace GameEditor.ActionEditor
{
    [InitializeOnLoad]
    internal static class ActionAssetInspector
    {
        private sealed class Document
        {
            internal readonly bool IsActionAsset;

            internal Document(TextAsset source) => IsActionAsset = AssetStorage.IsTimeline<ActionAsset>(source);
        }

        private static ConditionalWeakTable<TextAsset, Document> documents = new ConditionalWeakTable<TextAsset, Document>();

        static ActionAssetInspector()
        {
            Editor.finishedDefaultHeaderGUI += DrawOpenButton;
            EditorApplication.projectChanged += () => documents = new ConditionalWeakTable<TextAsset, Document>();
        }

        private static void DrawOpenButton(Editor editor)
        {
            if (editor.target is TextAsset source && editor.targets.Length == 1
                && documents.GetValue(source, asset => new Document(asset)).IsActionAsset
                && GUILayout.Button("Open Action Editor") && ActionEditorWindow.OpenAsset(source))
            {
                GUIUtility.ExitGUI();
            }
        }

        [MenuItem("Assets/Create/ActionAsset", false, 30)]
        private static void CreateAsset() => AssetStorage.Create<ActionAsset>("New ActionAsset");

        [MenuItem("Window/Action Editor")]
        private static void OpenWindow() => ActionEditorWindow.ShowWindow<ActionEditorWindow>();

        [OnOpenAsset]
        private static bool OpenTimeline(int instanceId, int line) =>
            EditorUtility.InstanceIDToObject(instanceId) is TextAsset source && ActionEditorWindow.OpenAsset(source);
    }
}
