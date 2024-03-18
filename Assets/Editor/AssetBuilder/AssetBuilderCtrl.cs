
using UnityEditor;
using UnityEngine;

namespace GameEditor.AssetBuidler
{
    public static class AssetBuilderCtrl
    {


        #region Build Asset

        [MenuItem("Assets/Game Editor/BuildPlayer", true)]
        static bool ValidBuildPlayer()
        {
            return true;
        }


        //简易的打包脚本，需要手动把res需要的资源移到对应路径下
        [MenuItem("Assets/Game Editor/BuildPlayer", false, 900)]
        public static void BuildPlayer()
        {
            BuildPlatformConfig config = AssetBuilderConfig.GetConfig(BuildPlatform.Win);

            PlayerSettings.companyName = "BlackRail";
            PlayerSettings.productName = "BlackRail";
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            PlayerSettings.SetScriptingBackend(config.buildTargetGroup, config.scriptingImplementation);

            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;

            BuildPipeline.BuildPlayer(config.defaultScene, config.buildLocationPathName, config.buildTarget, config.buildOption);

        }
        #endregion
    }
}