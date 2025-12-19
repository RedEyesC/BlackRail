using System;
using UnityEditor;
using UnityEngine;

namespace GameEditor.AssetBuidler
{
    class AssetAppBuildCtrl
    {
        public static void BuildAssetApp(
            BuildPlatform buildPlatform,
            AppPlatform appPlatform,
            AppResSource appResSource,
            AppResMode appResMode
        ) { }

        [MenuItem("Assets/Game Editor/BuildPlayer", true)]
        static bool ValidBuildPlayer()
        {
            return true;
        }

        public static void InitU3dConfig(BuildPlatform buildPlatform)
        {
            PlayerSettings.companyName = "BlackRail";
            PlayerSettings.productName = "BlackRail";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.colorSpace = ColorSpace.Gamma;

            PlayerSettings.bakeCollisionMeshes = true;

            PlayerSettings.stripEngineCode = false;
            PlayerSettings.stripUnusedMeshComponents = true;

            PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);

            QualitySettings.softParticles = false;

            //设置图形接口
            if (buildPlatform == BuildPlatform.Android)
            {
                UnityEngine.Rendering.GraphicsDeviceType[] deviceTypes = new UnityEngine.Rendering.GraphicsDeviceType[2];
                deviceTypes[0] = UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3;
                deviceTypes[1] = UnityEngine.Rendering.GraphicsDeviceType.Vulkan;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, deviceTypes);

                PlayerSettings.Android.resizableWindow = true;
                PlayerSettings.Android.renderOutsideSafeArea = false;
            }
            else if (buildPlatform == BuildPlatform.IOS)
            {
                UnityEngine.Rendering.GraphicsDeviceType[] deviceTypes = new UnityEngine.Rendering.GraphicsDeviceType[1];
                deviceTypes[0] = UnityEngine.Rendering.GraphicsDeviceType.Metal;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, deviceTypes);
            }
            else if (buildPlatform == BuildPlatform.Win)
            {
                UnityEngine.Rendering.GraphicsDeviceType[] deviceTypes = new UnityEngine.Rendering.GraphicsDeviceType[2];
                deviceTypes[0] = UnityEngine.Rendering.GraphicsDeviceType.Direct3D11;
                deviceTypes[1] = UnityEngine.Rendering.GraphicsDeviceType.Vulkan;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, deviceTypes);
            }
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
    }
}
