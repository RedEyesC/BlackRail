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


    public class ModelPerformance
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

        public ModelPerformance()
        {
        }
        public ModelPerformance(string name)
        {
            this.name = name;
        }
    }




    public class AssetsCompressionCtrl
    {

        public static void ExportData(DefaultAsset folder, PerformanceType type, string exportPath)
        {
            List<ModelPerformance> prefabPerformance = new List<ModelPerformance>();

            if (type == PerformanceType.Model)
            {

                //遍历文件夹下所有的prefab
                string path = AssetDatabase.GetAssetPath(folder);


                if (path == "")
                {
                    EditorUtility.DisplayDialog("error", "未选择文件夹", "确认");
                    return;
                }


                string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { path });

                HashSet<Mesh> meshHash = new HashSet<Mesh>();
                HashSet<Texture> textureHash = new HashSet<Texture>();
                Dictionary<string, float> bundleSizeMap = new Dictionary<string, float>();

                Dictionary<GameObject, List<Mesh>> prefabMesh = new Dictionary<GameObject, List<Mesh>>();
                Dictionary<GameObject, List<Texture>> prefabTexture = new Dictionary<GameObject, List<Texture>>();

                foreach (var guid in guids)
                {

                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    //mesh
                    var meshes = GetAllMeshes(prefab);
                    //贴图
                    var textures = GetAllTextures(prefab);


                    prefabMesh.Add(prefab, meshes);
                    prefabTexture.Add(prefab, textures);

                    meshHash.UnionWith(meshes);
                    textureHash.UnionWith(textures);

                }


                var objPaths = new List<string>();

                //复制Mesh到临时目录
                List<Mesh> meshList = meshHash.ToList();

                string tempMeshRootDir = "Assets/TempMesh";
                if (Directory.Exists(tempMeshRootDir))
                {
                    Directory.Delete(tempMeshRootDir, true);
                }

                Directory.CreateDirectory(tempMeshRootDir);

                var meshToCopyMesh = new Dictionary<string, string>();
                try
                {
                    AssetDatabase.StartAssetEditing();
                    for (int i = 0; i < meshList.Count; i++)
                    {
                        var mesh = meshList[i];
                        var cloneMesh = UnityEngine.Object.Instantiate(mesh);
                        cloneMesh.name = i.ToString();
                        var cloneMeshPath = tempMeshRootDir + "/" + i.ToString() + ".asset";
                        AssetDatabase.CreateAsset(cloneMesh, cloneMeshPath);
                        objPaths.Add(cloneMeshPath);
                        var meshPath = AssetDatabase.GetAssetPath(mesh);
                        meshToCopyMesh.Add(cloneMeshPath, meshPath);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                AssetDatabase.Refresh();


                List<Texture> textureList = textureHash.ToList();

                foreach (var texture in textureList)
                {
                    objPaths.Add(AssetDatabase.GetAssetPath(texture));
                }

                //计算ab包占用
                bundleSizeMap = CalculateStoreSize(objPaths);

                //shanc
                if (Directory.Exists(tempMeshRootDir))
                {
                    Directory.Delete(tempMeshRootDir, true);
                }


                //替换到实际路径
                foreach (var kv in meshToCopyMesh)
                {
                    var bundleSize = bundleSizeMap[kv.Key];
                    bundleSizeMap.Remove(kv.Key);

                    bundleSizeMap.Add(kv.Value, bundleSize);
                }

                //统计信息
                foreach (var kv in prefabMesh)
                {
                    GameObject obj = kv.Key;
                    ModelPerformance modelPerformance = new ModelPerformance(AssetDatabase.GetAssetPath(obj));
                    List<Mesh> meshs = kv.Value;
                    List<Texture> textures = prefabTexture[kv.Key];

                    foreach (Mesh mesh in meshs)
                    {
                        float bundleSize = bundleSizeMap[AssetDatabase.GetAssetPath(mesh)];

                        modelPerformance.bundleSize_mesh += bundleSize;
                        modelPerformance.memorySize_mesh = GetMeshRuntimeMemory(mesh);
                        modelPerformance.triangleCount = mesh.triangles.Length;
                    }


                    foreach (Texture texture in textures)
                    {
                        float bundleSize = bundleSizeMap[AssetDatabase.GetAssetPath(texture)];

                        modelPerformance.bundleSize_texture += bundleSize;
                        modelPerformance.memorySize_texture += GetTextureRuntimeMemory(texture);
                        modelPerformance.textureSize += texture.width * texture.height;
                    }

                    modelPerformance.bundleSize = modelPerformance.bundleSize_mesh + modelPerformance.bundleSize_texture;
                    modelPerformance.memorySize = modelPerformance.memorySize_mesh + modelPerformance.bundleSize_texture;

                    prefabPerformance.Add(modelPerformance);
                }


                //导出xml
                XmlEditor.XmlDataWriter.SaveListToXml(prefabPerformance, exportPath + "/" + path.Replace("/", "_") + "_OutFile.xml");

            }


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


        public static List<Mesh> GetAllMeshes(GameObject prefab)
        {
            var meshes = new List<Mesh>();
            var meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
            var skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var meshFilter in meshFilters)
            {
                var sharedMesh = meshFilter.sharedMesh;
                if (sharedMesh == null) continue;
                var mesh = sharedMesh;
                meshes.Add(mesh);
            }

            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                var sharedMesh = skinnedMeshRenderer.sharedMesh;
                if (sharedMesh == null) continue;
                var mesh = sharedMesh;
                meshes.Add(mesh);
            }

            return meshes;
        }

        public static List<Texture> GetAllTextures(GameObject prefab)
        {
            var textures = new List<Texture>();
            var renderers = prefab.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                foreach (var texture in GetTexturesByMaterialList(renderer.sharedMaterials))
                {
                    if (texture != null && !textures.Contains(texture))
                    {
                        textures.Add(texture);
                    }
                }
            }

            return textures;
        }

        public static List<Texture> GetTexturesByMaterialList(Material[] materials)
        {
            List<Texture> textures = new List<Texture>();
            foreach (var material in materials)
            {
                if (material == null)
                {
                    continue;
                }
                if (material.shader == null)
                {
                    Debug.Log($"Material {material.name} has no shader", material);
                    return textures;
                }

                var count = material.shader.GetPropertyCount();
                for (int i = 0; i < count; i++)
                {
                    var type = material.shader.GetPropertyType(i);
                    if (type == ShaderPropertyType.Texture)
                    {
                        var propertyName = material.shader.GetPropertyName(i);
                        Texture texture = material.GetTexture(propertyName);
                        if (texture != null && !textures.Contains(texture))
                        {
                            textures.Add(texture);
                        }
                    }
                }
            }

            return textures;
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

            return StoreSizeMap;
        }




    }
}
