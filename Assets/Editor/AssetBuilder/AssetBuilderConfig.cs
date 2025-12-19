using UnityEditor;

namespace GameEditor.AssetBuidler
{
    public enum BuildPlatform
    {
        Win,
        Android,
        IOS,
    };

    public enum AppResSource
    {
        _dev,
        _base,
    };

    public enum AppPlatform
    {
        Debug,
        Release,
    };

    public enum AppResMode
    {
        Small,
        Major,
        All,
    };

    public enum AssetSetting
    {
        Defalut,
    };

    public class BuildPlatformConfig
    {
        public string platformName;
        public string buildLocationPathName;
        public string assetTargetPathEx = "";
        public string pluginPath;
        public string appTargetPath;
        public string[] defaultScene = { "Assets/Init/Default.unity" };
        public ScriptingImplementation scriptingImplementation;
        public BuildTargetGroup buildTargetGroup;
        public BuildPlatform buildPlatform;
        public BuildTarget buildTarget;
        public BuildOptions buildOption = BuildOptions.StrictMode | BuildOptions.CompressWithLz4HC;
        public BuildAssetBundleOptions buildAssetBundleOptions;
    }

    public static class AssetBuilderConfig
    {
        private static BuildPlatformConfig[] mBuildConfig;

        static AssetBuilderConfig()
        {
            mBuildConfig = new BuildPlatformConfig[3];

            mBuildConfig[(int)BuildPlatform.Win] = new BuildPlatformConfig();
            mBuildConfig[(int)BuildPlatform.Win].platformName = "win";
            mBuildConfig[(int)BuildPlatform.Win].buildLocationPathName = "C:/Users/admin/Desktop/tool/tool.exe";
            mBuildConfig[(int)BuildPlatform.Win].buildPlatform = BuildPlatform.Win;
            mBuildConfig[(int)BuildPlatform.Win].buildTargetGroup = BuildTargetGroup.Standalone;
            mBuildConfig[(int)BuildPlatform.Win].buildTarget = BuildTarget.StandaloneWindows64;
            mBuildConfig[(int)BuildPlatform.Win].scriptingImplementation = ScriptingImplementation.IL2CPP;
            mBuildConfig[(int)BuildPlatform.Win].buildAssetBundleOptions =
                BuildAssetBundleOptions.ChunkBasedCompression
                | BuildAssetBundleOptions.AppendHashToAssetBundleName
                | BuildAssetBundleOptions.DeterministicAssetBundle;
        }

        public static BuildPlatformConfig GetConfig(BuildPlatform platform)
        {
            return mBuildConfig[(int)platform];
        }
    }
}
