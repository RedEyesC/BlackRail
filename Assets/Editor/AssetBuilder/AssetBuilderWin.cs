using GameEditor.AssetBuidler;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.AssetBuilder
{
    public class AssetsBuildWin : EditorWindow
    {
        public BuildPlatform buildPlatform = BuildPlatform.Win;
        public AppPlatform appPlatform = AppPlatform.Debug;
        public AppResSource appResSource = AppResSource._dev;
        public AppResMode appResMode = AppResMode.small;

        public DefaultAsset folderValue;

        public static string exportPath;

        [MenuItem("Tools/AssetsBuildWin")]
        private static void Open()
        {
            exportPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            ;
            GetWindow<AssetsBuildWin>().Close();
            GetWindow<AssetsBuildWin>().Show();
        }

        public void OnGUI()
        {
            GUILayout.BeginVertical("GroupBox");
            GUILayout.Label("设置", EditorStyles.boldLabel);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Build Platform:");
            buildPlatform = (BuildPlatform)EditorGUILayout.EnumPopup(buildPlatform);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("App Platform:");
            appPlatform = (AppPlatform)EditorGUILayout.EnumPopup(appPlatform);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("App Res Source:");
            appResSource = (AppResSource)EditorGUILayout.EnumPopup(appResSource);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("App Res Mode:");
            appResMode = (AppResMode)EditorGUILayout.EnumPopup(appResMode);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical("GroupBox");
            GUILayout.Label("功能", EditorStyles.boldLabel);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate AssetBundle"))
            {
                AssetBuilderCtrl.BuildAssetBundles(buildPlatform, appResSource);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Build App"))
            {
                AssetBuilderCtrl.BuildAssetApp(buildPlatform, appPlatform, appResSource, appResMode);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
