using System;
using UnityEditor;
using UnityEngine;

namespace GameEditor.AssetBuidler
{
    [Serializable]
    public class AssetBundleCollector
    {
        public string CollectPath = string.Empty;

        public PackRule PackRuleName = PackRule.PackCollector;

        public FilterRuleName FilterRuleName = FilterRuleName.All;
    }

    [Serializable]
    public class AssetBundleCollectorGroup
    {
        public string GroupName = "New Group";

        public AssetBundleCollector[] Collectors = Array.Empty<AssetBundleCollector>();
    }

    public class AssetBundleCollectorSetting : ScriptableObject
    {
        public string BuildPlatform = "Win";
        public string AppResSource = "_dev";
        public string BuildOutputRoot = "../_assets/res/";

        public BuildCompression CompressOption = BuildCompression.LZ4;

        public BuildAssetBundleOptions Options =
            BuildAssetBundleOptions.ChunkBasedCompression
            | BuildAssetBundleOptions.DisableLoadAssetByFileName
            | BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;

        public AssetBundleCollectorGroup[] Groups = Array.Empty<AssetBundleCollectorGroup>();
    }
}
