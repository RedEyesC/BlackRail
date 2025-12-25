using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEngine;

namespace GameEditor.AssetBuidler
{
    public static class AssetBundleBuildCtrl
    {
        public static AssetDependencyDatabase AssetDependencyDatabase;

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

            Dictionary<string, BuildBundleInfo> bundleInfoDic = CreateBuildMap(assetColloectorSetting);

            BuildingTask(assetColloectorSetting, bundleInfoDic);

            CreateManifest(assetColloectorSetting, bundleInfoDic);
        }

        public static void BuildTaskPrepare(AssetBundleCollectorSetting assetColloectorSetting)
        {
            string savePath = GetPipelineOutputDirectory(assetColloectorSetting);

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            AssetDependencyDatabase = new AssetDependencyDatabase(true, savePath);
        }

        public static string GetPipelineOutputDirectory(AssetBundleCollectorSetting assetColloectorSetting)
        {
            return string.Format(
                "{0}{1}{2}",
                assetColloectorSetting.BuildOutputRoot,
                assetColloectorSetting.AppResSource,
                assetColloectorSetting.BuildPlatform
            );
        }

        public static Dictionary<string, BuildBundleInfo> CreateBuildMap(AssetBundleCollectorSetting assetColloectorSetting)
        {
            Dictionary<string, CollectAssetInfo> allBuildAssetInfos = new Dictionary<string, CollectAssetInfo>(1000);

            List<CollectAssetInfo> allCollectAssets = BeginCollect(assetColloectorSetting);

            //录入所有收集器主动收集的资源
            foreach (var collectAssetInfo in allCollectAssets)
            {
                allBuildAssetInfos.Add(collectAssetInfo.AssetInfo.AssetPath, collectAssetInfo);
            }

            //填充所有收集资源的依赖列表,理论上所有依赖应该也已经被收集
            foreach (var collectAssetInfo in allCollectAssets)
            {
                var dependAssetInfos = new List<CollectAssetInfo>(collectAssetInfo.DependAssets.Count);
                foreach (var dependAsset in collectAssetInfo.DependAssets)
                {
                    if (allBuildAssetInfos.TryGetValue(dependAsset.AssetPath, out CollectAssetInfo value))
                        dependAssetInfos.Add(value);
                    else
                        throw new Exception("Should never get here !");
                }
                allBuildAssetInfos[collectAssetInfo.AssetInfo.AssetPath].DependAssetInfos = dependAssetInfos;
            }

            //构建资源列表
            Dictionary<string, BuildBundleInfo> bundleInfoDic = new Dictionary<string, BuildBundleInfo>(10000);

            var allPackAssets = allBuildAssetInfos.Values.ToList();
            foreach (var assetInfo in allPackAssets)
            {
                string bundleName = assetInfo.BundleName;
                if (string.IsNullOrEmpty(bundleName))
                    throw new Exception("Should never get here !");

                if (bundleInfoDic.TryGetValue(bundleName, out BuildBundleInfo bundleInfo))
                {
                    bundleInfo.PackAsset(assetInfo);
                }
                else
                {
                    BuildBundleInfo newBundleInfo = new BuildBundleInfo(bundleName);
                    newBundleInfo.PackAsset(assetInfo);
                    bundleInfoDic.Add(bundleName, newBundleInfo);
                }
            }

            return bundleInfoDic;
        }

        public static List<CollectAssetInfo> BeginCollect(AssetBundleCollectorSetting assetColloectorSetting)
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

            return result.Values.ToList();
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

                if (AssetBundleCollectorConfig.IsIgnore(assetInfo) == false)
                {
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
            string[] depends = AssetDependencyDatabase.GetDependencies(mainAssetPath, true);
            List<AssetInfo> result = new List<AssetInfo>(depends.Length);
            foreach (string assetPath in depends)
            {
                // 注意：排除主资源对象
                if (assetPath == mainAssetPath)
                    continue;

                AssetInfo assetInfo = new AssetInfo(assetPath);
                if (AssetBundleCollectorConfig.IsIgnore(assetInfo) == false)
                    result.Add(assetInfo);
            }
            return result;
        }

        private static void BuildingTask(
            AssetBundleCollectorSetting assetColloectorSetting,
            Dictionary<string, BuildBundleInfo> bundleInfoDic
        )
        {
            List<UnityEditor.AssetBundleBuild> bundleBuilds = new List<UnityEditor.AssetBundleBuild>(bundleInfoDic.Count);
            foreach (var bundleInfo in bundleInfoDic.Values)
            {
                bundleBuilds.Add(bundleInfo.CreatePipelineBuild());
            }

            var buildContent = new BundleBuildContent(bundleBuilds);

            IBundleBuildResults buildResults;
            var buildParameters = GetBundleBuildParameters(assetColloectorSetting);
            IList<IBuildTask> taskList = Create();
            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParameters, buildContent, out buildResults, taskList);
            if (exitCode < 0)
            {
                throw new Exception($"UnityEngine build failed ! ReturnCode : {exitCode}");
            }
        }

        public static IList<IBuildTask> Create()
        {
            var buildTasks = new List<IBuildTask>();

            // Setup
            buildTasks.Add(new SwitchToBuildPlatform());
            buildTasks.Add(new RebuildSpriteAtlasCache());

            // Player Scripts
            buildTasks.Add(new BuildPlayerScripts());
            buildTasks.Add(new PostScriptsCallback());

            // Dependency
            buildTasks.Add(new CalculateSceneDependencyData());
#if UNITY_2019_3_OR_NEWER
            buildTasks.Add(new CalculateCustomDependencyData());
#endif
            buildTasks.Add(new CalculateAssetDependencyData());
            buildTasks.Add(new StripUnusedSpriteSources());

            //if (string.IsNullOrEmpty(builtInShaderBundleName) == false)
            //    buildTasks.Add(new CreateBuiltInShadersBundle(builtInShaderBundleName));
            //if (string.IsNullOrEmpty(monoScriptsBundleName) == false)
            //    buildTasks.Add(new CreateMonoScriptBundle(monoScriptsBundleName));

            buildTasks.Add(new PostDependencyCallback());

            // Packing
            buildTasks.Add(new GenerateBundlePacking());
            buildTasks.Add(new UpdateBundleObjectLayout());
            buildTasks.Add(new GenerateBundleCommands());
            buildTasks.Add(new GenerateSubAssetPathMaps());
            buildTasks.Add(new GenerateBundleMaps());
            buildTasks.Add(new PostPackingCallback());

            // Writing
            buildTasks.Add(new WriteSerializedFiles());
            buildTasks.Add(new ArchiveAndCompressBundles());
            buildTasks.Add(new AppendBundleHash());
            buildTasks.Add(new GenerateLinkXml());
            buildTasks.Add(new PostWritingCallback());

            return buildTasks;
        }

        public static BundleBuildParameters GetBundleBuildParameters(AssetBundleCollectorSetting assetColloectorSetting)
        {
            BuildTarget buildTarget = BuildTarget.Android;

            if (assetColloectorSetting.BuildPlatform == BuildPlatform.Android.ToString())
            {
                buildTarget = BuildTarget.Android;
            }
            else if (assetColloectorSetting.BuildPlatform == BuildPlatform.Win.ToString())
            {
                buildTarget = BuildTarget.StandaloneWindows64;
            }
            else if (assetColloectorSetting.BuildPlatform == BuildPlatform.IOS.ToString())
            {
                buildTarget = BuildTarget.iOS;
            }

            var targetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(buildTarget);
            var pipelineOutputDirectory = GetPipelineOutputDirectory(assetColloectorSetting);
            var buildParams = new BundleBuildParameters(buildTarget, targetGroup, pipelineOutputDirectory);

            buildParams.BundleCompression = assetColloectorSetting.CompressOption;

            buildParams.UseCache = true;
            buildParams.WriteLinkXML = true;

            return buildParams;
        }

        private static void CreateManifest(
            AssetBundleCollectorSetting assetColloectorSetting,
            Dictionary<string, BuildBundleInfo> bundleInfoDic
        ) { }

        public static string GenerateVersion()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string versionStr = Convert.ToInt32(ts.TotalSeconds).ToString();
            return versionStr;
        }
    }
}
