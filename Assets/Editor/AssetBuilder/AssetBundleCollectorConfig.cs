using System;
using System.Collections.Generic;
using System.IO;
using TrackEditor;
using UnityEditor;

namespace GameEditor.AssetBuidler
{
    public class AssetInfo
    {
        public string AssetPath;
        public string AssetGUID;
        public System.Type AssetType;

        public AssetInfo(string assetPath)
        {
            AssetPath = assetPath;
            AssetGUID = UnityEditor.AssetDatabase.AssetPathToGUID(AssetPath);
            AssetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(AssetPath);

            if (AssetType == null)
            {
                throw new Exception($"Found invalid asset : {AssetPath}");
            }
        }
    }

    public class CollectAssetInfo
    {
        public string BundleName { private set; get; }

        public AssetInfo AssetInfo { private set; get; }

        public List<AssetInfo> DependAssets = new List<AssetInfo>();

        public CollectAssetInfo(string bundleName, AssetInfo assetInfo)
        {
            BundleName = bundleName;
            AssetInfo = assetInfo;
        }
    }

    public enum PackRule
    {
        PackCollector,
        PackTopDirectory,
        PackDirectory,
    };

    public enum FilterRuleName
    {
        All,
        Prefab,
        Scene,
    };

    public delegate string PackRuleResult(string assetPath, string collectPath, string groupName);

    public static class AssetBundleCollectorConfig
    {
        public static Dictionary<PackRule, PackRuleResult> PackRuleFunc = new Dictionary<PackRule, PackRuleResult>
        {
            {
                PackRule.PackCollector,
                (string assetPath, string collectPath, string groupName) =>
                {
                    string bundleName;
                    if (AssetDatabase.IsValidFolder(collectPath))
                    {
                        bundleName = collectPath;
                    }
                    else
                    {
                        bundleName = RemoveExtension(collectPath);
                    }

                    return GetRegularPath(bundleName);
                }
            },
            {
                PackRule.PackTopDirectory,
                (string assetPath, string collectPath, string groupName) =>
                {
                    string assetSplitPath = assetPath.Replace(collectPath, string.Empty);
                    assetSplitPath = assetSplitPath.TrimStart('/');
                    string[] splits = assetSplitPath.Split('/');
                    if (splits.Length > 0)
                    {
                        if (Path.HasExtension(splits[0]))
                            throw new Exception($"Not found root directory : {assetSplitPath}");
                        string bundleName = $"{collectPath}/{splits[0]}";
                        return GetRegularPath(bundleName);
                    }
                    else
                    {
                        throw new Exception($"Not found root directory : {assetPath}");
                    }
                }
            },
            {
                PackRule.PackDirectory,
                (string assetPath, string collectPath, string groupName) =>
                {
                    string bundleName = Path.GetDirectoryName(assetPath);
                    return GetRegularPath(bundleName);
                }
            },
        };

        public static string GetRegularPath(string path)
        {
            return path.Replace('\\', '/').Replace("\\", "/"); //替换为Linux路径格式
        }

        public static string RemoveExtension(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            int index = str.LastIndexOf('.');
            if (index == -1)
                return str;
            else
                return str.Remove(index); //"assets/config/test.unity3d" --> "assets/config/test"
        }
    }
}
