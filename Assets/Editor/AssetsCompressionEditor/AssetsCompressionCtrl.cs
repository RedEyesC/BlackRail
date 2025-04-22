using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace GameEditor.AssetsCompressionEditor
{
    public enum PerformanceType
    {
        Model,
        Scene,
        Effect
    }


    public class CostInfo
    {
        public string name = "";
        public float bundleSize = 0;
        public float bundleSize_mesh = 0;
        public float bundleSize_texture = 0;
        public float memorySize = 0;
        public float memorySize_mesh = 0;
        public float memorySize_texture = 0;
        public int triangleCount = 0;
        public int textureSize = 0;

        public CostInfo()
        {
        }
        public CostInfo(string name)
        {
            this.name = name;
        }
    }


    public class AssetsCompressionCtrl
    {

        public static void ExportData(DefaultAsset folder, PerformanceType type, string exportPath)
        {
          

            //遍历文件夹下所有的prefab
            string path = AssetDatabase.GetAssetPath(folder);


            if (path == "")
            {
                EditorUtility.DisplayDialog("error", "未选择文件夹", "确认");
                return;
            }


            if (type == PerformanceType.Model)
            {

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { path });
                ExportDataSceneAndModel(guids, type, exportPath, path);

            }
            else if (type == PerformanceType.Scene)
            {

                List<string> allScenePaths = new List<string>();
                string[] guids = AssetDatabase.FindAssets("t:scene", new string[] { path });

                ExportDataSceneAndModel(guids, type, exportPath, path);

            }


        }


        public static void ExportDataSceneAndModel(string[] guids, PerformanceType type, string exportPath,string folderPath)
        {
            List<CostInfo> costInfoList = new List<CostInfo>();
            HashSet<string> depHash = new HashSet<string>();
            Dictionary<string, float> bundleSizeMap = new Dictionary<string, float>();

            Dictionary<string, List<string>> assetMesh = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> assetTexture = new Dictionary<string, List<string>>();

            foreach (var guid in guids)
            {

                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                //mesh
                var meshes = GetAllMeshes(assetPath);
                //贴图
                var textures = GetAllTextures(assetPath);


                assetMesh.Add(assetPath, meshes);
                assetTexture.Add(assetPath, textures);

                depHash.UnionWith(meshes);
                depHash.UnionWith(textures);

            }

            var depPaths = depHash.ToList();

            //计算ab包占用
            bundleSizeMap = CalculateStoreSize(depPaths);

            //统计信息
            foreach (var kv in assetMesh)
            {
                CostInfo CostInfo = new CostInfo(kv.Key);
                List<string> meshs = kv.Value;
                List<string> textures = assetTexture[kv.Key];

                foreach (string meshPath in meshs)
                {
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                    float bundleSize = bundleSizeMap[meshPath];
                    CostInfo.bundleSize_mesh += bundleSize;
                    CostInfo.memorySize_mesh += GetMeshRuntimeMemory(mesh);
                    CostInfo.triangleCount += mesh.triangles.Length / 3;
                }


                foreach (string texturePath in textures)
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

                    float bundleSize = bundleSizeMap[texturePath];
                    CostInfo.bundleSize_texture += bundleSize;
                    CostInfo.memorySize_texture += GetTextureRuntimeMemory(texture);
                    CostInfo.textureSize += texture.width * texture.height;
                }

                CostInfo.bundleSize = CostInfo.bundleSize_mesh + CostInfo.bundleSize_texture;
                CostInfo.memorySize = CostInfo.memorySize_mesh + CostInfo.bundleSize_texture;

                costInfoList.Add(CostInfo);
            }

            //导出xml
            XmlEditor.XmlDataWriter.SaveListToXml(costInfoList, exportPath + "/" + folderPath.Replace("/", "_") + "_OutFile.xml");

        }




        public static float GetTextureRuntimeMemory(string texturePath)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            return GetTextureRuntimeMemory(texture);

        }


        public static float GetTextureRuntimeMemory(Texture texture)
        {
            var textureUtilType = typeof(TextureImporter).Assembly.GetType("UnityEditor.TextureUtil");
            var getStorageMemorySizeLongMethod = textureUtilType.GetMethod("GetStorageMemorySizeLong", BindingFlags.Static | BindingFlags.Public);
            var storeSizeLong = (long)getStorageMemorySizeLongMethod.Invoke(null, new object[] { texture });
            var storeSize = storeSizeLong * 1.0f;
            storeSize /= 1024f * 1024f;
            return storeSize;
        }


        public static float GetMeshRuntimeMemory(string meshPath)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            return GetMeshRuntimeMemory(mesh);

        }
          
        public static float GetMeshRuntimeMemory(Mesh mesh)
        {
            //由于attributes只会得到已经拥有的属性，所以这里不需要担心mesh的属性会变化
            VertexAttributeDescriptor[] attributes = mesh.GetVertexAttributes();
            var size = 0f;
            size += mesh.indexFormat == IndexFormat.UInt16 ? mesh.triangles.Length * 2 : mesh.triangles.Length * 4;
            foreach (var attr in attributes)
            {
                // 获取属性格式,如Float32、Float16等
                var format = attr.format;
                // 获取维度(如xyz为3维)
                int dimension = attr.dimension;
                //将format的数字部分提取出来，然后除以8，得到这个格式的字节数量。
                int bit = int.Parse(Regex.Replace(format.ToString(), @"[^\d]", "")) / 8;
                //计算字节数，单通道的字节数乘以维度
                var bytes = bit * dimension;
                //计算总字节数
                size += mesh.vertexCount * bytes;
            }

            //开始读写，占用翻倍
            if (mesh.isReadable)
            {
                size = 2 * size;
            }

            return size / 1024 / 1024;
        }

        public static List<string> GetAllMeshes(string assetPath)
        {
            return GetAllMeshes(new List<string> { assetPath });
        }


        public static List<string> GetAllMeshes(List<string> assetPaths)
        {
            //要求是依赖mesh这个类型的资源，假如是通过fbx引用的mesh在这里是无法统计到的，正常设计的时候在res资源里不应该直接引用fbx
            var meshes = GetCloneableDependencies(assetPaths, (path, type) =>
            {
                if (type == typeof(Mesh)) return true;
                return false;
            });

            return meshes;
        }

        public static List<string> GetAllTextures(string assetPath)
        {
            return GetAllTextures(new List<string> { assetPath });
        }


        public static List<string> GetAllTextures(List<string> assetPaths)
        {
            var textures = GetCloneableDependencies(assetPaths, (path, type) =>
            {
                if (type == typeof(Texture) || type == typeof(Texture2D)) return true;
                return false;
            });

            return textures;
        }


        public static List<string> GetCloneableDependencies(List<string> assetPaths, Func<string, System.Type, bool> filterFunc = null)
        {
            var dependencies = new List<string>();
            var allDependencies = AssetDatabase.GetDependencies(assetPaths.ToArray(), true);

            HashSet<string> assetPathSet = new HashSet<string>();
            foreach (var assetPath in assetPaths)
            {
                assetPathSet.Add(assetPath);
            }

            foreach (var dependency in allDependencies)
            {
                var mainAssetTypeAtPath = AssetDatabase.GetMainAssetTypeAtPath(dependency);
                if (mainAssetTypeAtPath == null) continue;
                if (assetPathSet.Contains(dependency)) continue;
                if (filterFunc != null && !filterFunc(dependency, mainAssetTypeAtPath)) continue;
                dependencies.Add(dependency);
            }

            return dependencies;
        }


        public static Dictionary<string, float> CalculateStoreSize(List<string> objPaths)
        {

            int assetCount = objPaths.Count;
            int assetIndex = 0;

            AssetBundleBuild[] builds = new AssetBundleBuild[assetCount];

            for (int i = 0; i < objPaths.Count; i++)
            {
                builds[assetIndex] = new AssetBundleBuild();
                builds[assetIndex].assetBundleName = assetIndex.ToString();
                builds[assetIndex].assetBundleVariant = "ab";

                builds[assetIndex].assetNames = new string[] { objPaths[i] };

                assetIndex++;
            }

            var assetBundleDirectory = "./TempAssetBundles";
            if (Directory.Exists(assetBundleDirectory))
            {
                Directory.Delete(assetBundleDirectory, true);
            }

            Directory.CreateDirectory(assetBundleDirectory);

            BuildAssetBundleOptions buildAssetBundleOptions =
                BuildAssetBundleOptions.ChunkBasedCompression |
                BuildAssetBundleOptions.DisableLoadAssetByFileName |
                BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
            BuildPipeline.BuildAssetBundles(assetBundleDirectory, builds, buildAssetBundleOptions, BuildTarget.StandaloneWindows64);

            var StoreSizeMap = new Dictionary<string, float>();

            var assetBundlePaths = Directory.GetFiles(assetBundleDirectory, "*.ab");
            foreach (var assetBundlePath in assetBundlePaths)
            {
                var bundleName = Path.GetFileNameWithoutExtension(assetBundlePath);
                var bundleSize = (int)(new FileInfo(assetBundlePath).Length) * 1.0f;
                bundleSize /= (1024f * 1024f);
                StoreSizeMap.Add(objPaths[int.Parse(bundleName)], bundleSize);
            }

            //删除temp文件
            if (Directory.Exists(assetBundleDirectory))
            {
                Directory.Delete(assetBundleDirectory, true);
            }

            return StoreSizeMap;
        }




    }
}
