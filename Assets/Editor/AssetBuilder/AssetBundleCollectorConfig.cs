using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace GameEditor.AssetBuidler
{
    public class AssetInfo
    {
        private string _fileExtension = null;

        public string FileExtension
        {
            get
            {
                if (string.IsNullOrEmpty(_fileExtension))
                    _fileExtension = System.IO.Path.GetExtension(AssetPath);
                return _fileExtension;
            }
        }

        public string AssetPath;
        public string AssetGUID;
        public System.Type AssetType;

        public AssetInfo(string assetPath)
        {
            AssetPath = assetPath;
            AssetGUID = AssetDatabase.AssetPathToGUID(AssetPath);
            AssetType = AssetDatabase.GetMainAssetTypeAtPath(AssetPath);

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

        public List<CollectAssetInfo> DependAssetInfos = new List<CollectAssetInfo>();

        public CollectAssetInfo(string bundleName, AssetInfo assetInfo)
        {
            BundleName = bundleName;
            AssetInfo = assetInfo;
        }
    }

    public class BuildBundleInfo
    {
        private readonly Dictionary<string, CollectAssetInfo> _packAssetDic = new Dictionary<string, CollectAssetInfo>(100);

        public readonly List<CollectAssetInfo> AllPackAssets = new List<CollectAssetInfo>(100);

        public string BundleName;

        public BuildBundleInfo(string bundleName)
        {
            BundleName = bundleName;
        }

        public void PackAsset(CollectAssetInfo buildAsset)
        {
            string assetPath = buildAsset.AssetInfo.AssetPath;
            if (_packAssetDic.ContainsKey(assetPath))
                return;

            _packAssetDic.Add(assetPath, buildAsset);
            AllPackAssets.Add(buildAsset);
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

        public static readonly HashSet<string> IgnoreFileExtensions = new HashSet<string>()
        {
            "",
            ".so",
            ".cs",
            ".js",
            ".boo",
            ".meta",
            ".cginc",
            ".hlsl",
        };

        public static bool IsIgnore(AssetInfo assetInfo)
        {
            if (assetInfo.AssetPath.StartsWith("Assets/") == false && assetInfo.AssetPath.StartsWith("Packages/") == false)
            {
                UnityEngine.Debug.LogError($"Invalid asset path : {assetInfo.AssetPath}");
                return true;
            }

            // 忽略文件夹
            if (AssetDatabase.IsValidFolder(assetInfo.AssetPath))
                return true;

            // 忽略编辑器图标资源
            if (assetInfo.AssetPath.Contains("/Gizmos/"))
                return true;

            // 忽略编辑器专属资源
            if (assetInfo.AssetPath.Contains("/Editor/") || assetInfo.AssetPath.Contains("/Editor Resources/"))
                return true;

            // 忽略编辑器下的类型资源
            if (assetInfo.AssetType == typeof(LightingDataAsset))
                return true;
            if (assetInfo.AssetType == typeof(LightmapParameters))
                return true;

            // 忽略Unity引擎无法识别的文件
            if (assetInfo.AssetType == typeof(UnityEditor.DefaultAsset))
            {
                UnityEngine.Debug.LogWarning($"Cannot pack default asset : {assetInfo.AssetPath}");
                return true;
            }

            return IgnoreFileExtensions.Contains(assetInfo.FileExtension);
        }
    }
}
