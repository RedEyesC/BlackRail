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
        public AppResMode appResMode = AppResMode.Small;
        public AssetSetting assetSetting = AssetSetting.Defalut;

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
            GUILayout.Label("通用设置", EditorStyles.boldLabel);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Build Platform:");
            buildPlatform = (BuildPlatform)EditorGUILayout.EnumPopup(buildPlatform);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Res Source:");
            appResSource = (AppResSource)EditorGUILayout.EnumPopup(appResSource);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical("GroupBox");
            GUILayout.Label("App", EditorStyles.boldLabel);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("App Res Mode:");
            appResMode = (AppResMode)EditorGUILayout.EnumPopup(appResMode);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("App Platform:");
            appPlatform = (AppPlatform)EditorGUILayout.EnumPopup(appPlatform);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Build App"))
            {
                AssetAppBuildCtrl.BuildAssetApp(buildPlatform, appPlatform, appResSource, appResMode);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical("GroupBox");
            GUILayout.Label("Bundle", EditorStyles.boldLabel);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("AssetSetting Name:");
            assetSetting = (AssetSetting)EditorGUILayout.EnumPopup(assetSetting);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate AssetBundle"))
            {
                AssetBundleBuildCtrl.BuildAssetBundles(buildPlatform, appResSource, assetSetting);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical("GroupBox");
            GUILayout.Label("其他", EditorStyles.boldLabel);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create AssetSetting"))
            {
                AssetBundleBuildCtrl.CreateAssetSetting();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
