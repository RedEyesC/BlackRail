
using UnityEditor;
using UnityEngine;

namespace GameEditor.AssetsCompressionEditor
{


    public class AssetsCompressionWin : EditorWindow
    {

        public PerformanceType type = PerformanceType.Model;
        public DefaultAsset folderValue;

        public static string exportPath;

        [MenuItem("Tool/AssetsCompressionWin")]
        private static void Open()
        {
            exportPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6); ;
            GetWindow<AssetsCompressionWin>().Close();
            GetWindow<AssetsCompressionWin>().Show();
        }

        public void OnGUI()
        {
            GUILayout.BeginVertical("GroupBox");
            GUILayout.Label("资源开销收集器", EditorStyles.boldLabel);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("检索类型:");
            type = (PerformanceType)EditorGUILayout.EnumPopup(type);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("检索文件夹:");
            folderValue = EditorGUILayout.ObjectField(folderValue, typeof(DefaultAsset), true) as DefaultAsset;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导出路径:", exportPath);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("选择导出路径"))
            {
                exportPath = EditorUtility.OpenFolderPanel("选择导出路径", exportPath, "");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("导出开销数据"))
            {
                AssetsCompressionCtrl.ExportData(folderValue, type, exportPath);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

    }
}
