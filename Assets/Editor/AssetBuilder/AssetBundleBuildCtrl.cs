using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameEditor.AssetBuidler
{
    public static class AssetBundleBuildCtrl
    {
        public static void CreateAssetSetting()
        {
            AssetBundleCollectorSetting asset = ScriptableObject.CreateInstance<AssetBundleCollectorSetting>();
            string fullPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Editor/AssetBuilder/New Custom Asset.asset");
            ProjectWindowUtil.CreateAsset(asset, fullPath);
        }

        public static void BuildAssetBundles(BuildPlatform buildPlatform, AppResSource appResSource, AssetSetting assetSetting)
        {
            //string mVersionStr = GenerateVersion();

            AssetBundleCollectorSetting assetColloectorSetting = AssetDatabase.LoadAssetAtPath<AssetBundleCollectorSetting>(
                "Assets/Editor/AssetBuilder/" + assetSetting.ToString()
            );

            assetColloectorSetting.BuildPlatform = buildPlatform.ToString();
            assetColloectorSetting.AppResSource = appResSource.ToString();

            //开始构建流程
            BuildTaskPrepare(assetColloectorSetting);

            CreateBuildMap(assetColloectorSetting);
        }

        public static void BuildTaskPrepare(AssetBundleCollectorSetting assetColloectorSetting)
        {
            string savePath = string.Format(
                "{0}{1}{2}",
                assetColloectorSetting.BuildOutputRoot,
                assetColloectorSetting.AppResSource,
                assetColloectorSetting.BuildPlatform
            );

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
        }

        public static void CreateBuildMap(AssetBundleCollectorSetting assetColloectorSetting)
        {
            Dictionary<string, CollectAssetInfo> result = new Dictionary<string, CollectAssetInfo>(10000);

            AssetBundleCollectorGroup[] groups = assetColloectorSetting.Groups;
            //遍历group
            foreach (var group in groups)
            {
                var temper = GetAllCollectAssets(group);
                foreach (var collectAsset in temper)
                {
                    if (result.ContainsKey(collectAsset.AssetInfo.AssetPath) == false)
                        result.Add(collectAsset.AssetInfo.AssetPath, collectAsset);
                    else
                        throw new Exception($"The collecting asset file is existed : {collectAsset.AssetInfo.AssetPath}");
                }
            }
        }

        public static List<CollectAssetInfo> GetAllCollectAssets(AssetBundleCollectorGroup group)
        {
            Dictionary<string, CollectAssetInfo> result = new Dictionary<string, CollectAssetInfo>(10000);

            //遍历Collectors
            foreach (var collector in group.Collectors)
            {
                var temper = GetAllCollectAssets(collector, group);
                foreach (var collectAsset in temper)
                {
                    if (result.ContainsKey(collectAsset.AssetInfo.AssetPath) == false)
                        result.Add(collectAsset.AssetInfo.AssetPath, collectAsset);
                    else
                        throw new Exception(
                            $"The collecting asset file is existed : {collectAsset.AssetInfo.AssetPath} in group : {group.GroupName}"
                        );
                }
            }

            // 返回列表
            return result.Values.ToList();
        }

        public static List<CollectAssetInfo> GetAllCollectAssets(AssetBundleCollector collector, AssetBundleCollectorGroup group)
        {
            Dictionary<string, CollectAssetInfo> result = new Dictionary<string, CollectAssetInfo>(1000);

            // 收集打包资源路径
            List<string> findAssets = new List<string>();
            if (AssetDatabase.IsValidFolder(collector.CollectPath))
            {
                string searchFolder = collector.CollectPath;
                string[] findResult = FindAssets(collector.FilterRuleName.ToString(), searchFolder);
                findAssets.AddRange(findResult);
            }
            else
            {
                string assetPath = collector.CollectPath;
                findAssets.Add(assetPath);
            }

            // 收集打包资源信息
            foreach (string assetPath in findAssets)
            {
                var assetInfo = new AssetInfo(assetPath);

                if (result.ContainsKey(assetPath) == false)
                {
                    var collectAssetInfo = CreateCollectAssetInfo(collector, group, assetInfo);
                    result.Add(assetPath, collectAssetInfo);
                }
                else
                {
                    throw new Exception($"The collecting asset file is existed : {assetPath} in collector : {collector.CollectPath}");
                }
            }

            // 返回列表
            return result.Values.ToList();
        }

        public static string[] FindAssets(string searchType, string searchInFolder)
        {
            return FindAssets(searchType, new string[] { searchInFolder });
        }

        public static string[] FindAssets(string searchType, string[] searchInFolders)
        {
            // 注意：AssetDatabase.FindAssets()不支持末尾带分隔符的文件夹路径
            for (int i = 0; i < searchInFolders.Length; i++)
            {
                string folderPath = searchInFolders[i];
                searchInFolders[i] = folderPath.TrimEnd('/');
            }

            // 注意：获取指定目录下的所有资源对象（包括子文件夹）
            string[] guids;
            if (searchType == FilterRuleName.All.ToString())
                guids = AssetDatabase.FindAssets(string.Empty, searchInFolders);
            else
                guids = AssetDatabase.FindAssets($"t:{searchType}", searchInFolders);

            // 注意：AssetDatabase.FindAssets()可能会获取到重复的资源
            HashSet<string> result = new HashSet<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (result.Contains(assetPath) == false)
                {
                    result.Add(assetPath);
                }
            }

            // 返回结果
            return result.ToArray();
        }

        private static CollectAssetInfo CreateCollectAssetInfo(
            AssetBundleCollector collector,
            AssetBundleCollectorGroup group,
            AssetInfo assetInfo
        )
        {
            string bundleName = GetBundleName(collector, group, assetInfo);
            CollectAssetInfo collectAssetInfo = new CollectAssetInfo(bundleName, assetInfo);
            collectAssetInfo.DependAssets = GetAllDependencies(assetInfo.AssetPath);
            return collectAssetInfo;
        }

        private static string GetBundleName(AssetBundleCollector collector, AssetBundleCollectorGroup group, AssetInfo assetInfo)
        {
            PackRuleResult packRuleInstance = AssetBundleCollectorConfig.PackRuleFunc[collector.PackRuleName];
            return packRuleInstance(assetInfo.AssetPath, collector.CollectPath, group.GroupName);
        }

        private static List<AssetInfo> GetAllDependencies(string mainAssetPath)
        {
            //string[] depends = command.AssetDependency.GetDependencies(mainAssetPath, true);
            //List<AssetInfo> result = new List<AssetInfo>(depends.Length);
            //foreach (string assetPath in depends)
            //{
            //    // 注意：排除主资源对象
            //    if (assetPath == mainAssetPath)
            //        continue;

            //    AssetInfo assetInfo = new AssetInfo(assetPath);
            //    if (command.IgnoreRule.IsIgnore(assetInfo) == false)
            //        result.Add(assetInfo);
            //}
            //return result;
            return null;
        }

        public static string GenerateVersion()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string versionStr = Convert.ToInt32(ts.TotalSeconds).ToString();
            return versionStr;
        }
    }
}
