using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameEditor.ModelEditor
{
    public class ModelEditor
    {
        static readonly string ModelRawPath = "Assets/RawData/Model/";
        static readonly string ModelResPath = "Assets/Resource/Model/";

        static readonly string AnimRawPath = "Assets/RawData/Anim/";
        static readonly string AnimResPath = "Assets/Resource/Anim/";

        [MenuItem("Assets/Game Editor/导出模型", false, 900)]
        static void ExportModelInfo()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                ExportModelInfo(path);
            }
        }

        [MenuItem("Assets/Game Editor/导出模型", true)]
        static bool ValidExportModelInfo()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (path.Contains(ModelRawPath) && AssetDatabase.IsValidFolder(path))
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        [MenuItem("Assets/Game Editor/导出动画", false, 900)]
        static void ExportModelAnim()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                ExportModelAnim(path);
            }
        }

        [MenuItem("Assets/Game Editor/导出动画", true)]
        static bool ValidExportModelAnim()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(path))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(path);
                    foreach (var file in dirInfo.GetFiles())
                    {
                        if (file.Extension.ToLower() == ".fbx")
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    continue;
                }
            }
            return false;
        }

        public static void ExportModelAnim(string path)
        {
            string animType = GetAnimType(path);
            string savePath = AnimResPath + animType;

            DirectoryInfo dirInfo = new DirectoryInfo(path);
            foreach (var file in dirInfo.GetFiles())
            {
                if (file.Extension.ToLower() == ".fbx")
                {
                    string rawPath = path + "/" + file.Name;

                    UnityEngine.Object[] objs = AssetDatabase.LoadAllAssetsAtPath(rawPath);
                    foreach (UnityEngine.Object o in objs)
                    {
                        if (o is AnimationClip)
                        {
                            //fbx内存在一部分不会在unity显示的，需要剔除
                            if (o.name.Contains("_preview"))
                            {
                                continue;
                            }

                            AnimationClip clip = (AnimationClip)o;
                            AnimationClip newClip = new AnimationClip();

                            EditorUtility.CopySerialized(clip, newClip);
                            CreateFolder(savePath);
                            string resAnimPath = savePath + "/" + clip.name + ".anim";
                            CreateAsset(newClip, resAnimPath);
                        }
                    }
                }
            }

            Debug.LogFormat("{0} Export Anim Success！！", path);
        }

        static string GetAnimType(string path)
        {
            if (!path.Contains(AnimRawPath))
                return null;

            path = path.Replace(AnimRawPath, "");

            int index = path.IndexOf("/");
            return index >= 0 ? path.Substring(0, index) : path;
        }

        public static void ExportModelInfo(string path)
        {
            GameObject go = null;

            string modelType = GetModelType(path);
            string modelName = GetModelName(path);

            string prefabPath = path + "/Prefab";

            DirectoryInfo dirInfo = new DirectoryInfo(prefabPath);
            foreach (var file in dirInfo.GetFiles())
            {
                if (file.Extension.ToLower() == ".prefab")
                {
                    GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "/" + file.Name);
                    go = GameObject.Instantiate(pref);
                    go.name = Path.GetFileNameWithoutExtension(modelName);
                    break;
                }
            }

            string avatarResPath = ModelResPath + modelType + "/" + modelName + "/Avatar/";
            ExportAvatar(go, avatarResPath);

            string meshResPath = ModelResPath + modelType + "/" + modelName + "/Mesh/";
            ExportMesh(go, meshResPath);

            string textureResPath = ModelResPath + modelType + "/" + modelName + "/Material/";
            ExportMaterial(go, path, ModelResPath, textureResPath);

            string resPrefabPath = ModelResPath + modelType + "/" + modelName + "/";
            ExportModel(go, path, resPrefabPath);

            GameObject.DestroyImmediate(go);

            Debug.LogFormat("{0} Export Model Success！！", modelType + "/" + modelName);
        }

        public static string GetModelName(string path)
        {
            if (path.Contains(ModelRawPath))
            {
                path = path.Replace(ModelRawPath, "");
                path = path.Remove(0, path.IndexOf("/") + 1);

                if (path.Contains("/"))
                    path = path.Remove(path.IndexOf("/"));

                return path;
            }
            return null;
        }

        static string GetModelType(string path)
        {
            if (path.Contains(ModelRawPath))
            {
                path = path.Replace(ModelRawPath, "");
                path = path.Remove(path.IndexOf("/"));
                if (path.Contains("/"))
                    path = path.Remove(path.IndexOf("/"));
                return path;
            }
            return null;
        }

        static void ExportAvatar(GameObject go, string savePath)
        {
            Avatar avatar = null;

            Animator anim = go.GetComponentInChildren<Animator>();
            if (anim)
                avatar = anim.avatar;

            if (avatar == null)
                return;

            Avatar newAvatar = GameObject.Instantiate<Avatar>(avatar);
            string newAvPath = savePath + avatar.name + ".asset";

            CreateFolder(newAvPath.Remove(newAvPath.LastIndexOf("/")));
            CreateAsset(newAvatar, newAvPath);

            anim.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(newAvPath);
        }

        static void ExportMesh(GameObject go, string savePath)
        {
            Dictionary<string, string> meshMap = new Dictionary<string, string>();

            SkinnedMeshRenderer[] skRendererArr = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < skRendererArr.Length; i++)
            {
                var skinMeshRender = skRendererArr[i];
                Mesh mesh = skinMeshRender.sharedMesh;
                string meshPath = savePath + mesh.name + ".asset";
                meshMap.Add(mesh.name, meshPath);
                ExportMeshAsset(mesh, meshPath);
                skRendererArr[i].sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            }

            MeshFilter[] meshFltArr = go.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < meshFltArr.Length; i++)
            {
                var meshFilter = meshFltArr[i];
                Mesh mesh = meshFilter.sharedMesh;
                string meshPath = savePath + mesh.name + ".asset";
                ExportMeshAsset(mesh, meshPath);
                meshFltArr[i].sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            }
        }

        static void ExportMeshAsset(Mesh mesh, string meshPath)
        {
            Mesh newMesh = GameObject.Instantiate<Mesh>(mesh);

            CreateFolder(meshPath.Remove(meshPath.LastIndexOf("/")));
            CreateAsset(newMesh, meshPath);
        }

        static void ExportMaterial(GameObject go, string rawPath, string savePath, string textureResPath)
        {
            Dictionary<Material, Material> exportedMaterials = new Dictionary<Material, Material>();

            Renderer[] rendererArr = go.GetComponentsInChildren<Renderer>();
            for (int k = 0; k < rendererArr.Length; k++)
            {
                Material[] mats = rendererArr[k].sharedMaterials;
                Material[] newMats = new Material[mats.Length];
                for (int j = 0; j < mats.Length; j++)
                {
                    Material mat = mats[j];
                    if (exportedMaterials.ContainsKey(mat))
                    {
                        newMats[j] = exportedMaterials[mat];
                        continue;
                    }

                    Material newMat = GameObject.Instantiate<Material>(mat);

                    string newMatPath = textureResPath + mat.name + ".mat";

                    CreateFolder(newMatPath.Remove(newMatPath.LastIndexOf("/")));
                    AssetDatabase.CreateAsset(newMat, newMatPath);

                    var resShader = newMat.shader;

                    int propertyCount = ShaderUtil.GetPropertyCount(resShader);
                    for (int i = 0; i < propertyCount; i++)
                    {
                        if (ShaderUtil.GetPropertyType(resShader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                        {
                            string propertyName = ShaderUtil.GetPropertyName(resShader, i);

                            Texture tex = newMat.GetTexture(propertyName);

                            if (tex == null)
                                continue;

                            ExportTextureAsset(newMat, propertyName, tex, textureResPath);
                        }
                    }

                    newMat = AssetDatabase.LoadAssetAtPath<Material>(newMatPath);
                    ClearUnusedProperties(newMat);
                    newMats[j] = newMat;
                    exportedMaterials.Add(mat, newMat);
                }
                rendererArr[k].sharedMaterials = newMats;
            }

            AssetDatabase.Refresh();
        }

        static void ClearUnusedProperties(Material mat)
        {
            if (mat)
            {
                SerializedObject psSource = new SerializedObject(mat);
                SerializedProperty emissionProperty = psSource.FindProperty("m_SavedProperties");
                SerializedProperty texEnvs = emissionProperty.FindPropertyRelative("m_TexEnvs");
                SerializedProperty floats = emissionProperty.FindPropertyRelative("m_Floats");
                SerializedProperty colos = emissionProperty.FindPropertyRelative("m_Colors");
                CleanMaterialSerializedProperty(texEnvs, mat);
                CleanMaterialSerializedProperty(floats, mat);
                CleanMaterialSerializedProperty(colos, mat);
                psSource.ApplyModifiedProperties();
                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
        }

        static void CleanMaterialSerializedProperty(SerializedProperty property, Material mat)
        {
            var shader = mat.shader;
            var count = property.arraySize;
            for (int i = count - 1; i >= 0; i--)
            {
                //在shader内找不到的属性，删除
                var prop = property.GetArrayElementAtIndex(i);
                if (shader.FindPropertyIndex(prop.FindPropertyRelative("first").stringValue) < 0)
                {
                    property.DeleteArrayElementAtIndex(i);
                }
            }
        }

        static bool ExportTextureAsset(Material mat, string propertyName, Texture tex, string textureResPath)
        {
            var texPath = AssetDatabase.GetAssetPath(tex);
            texPath = texPath.Replace("\\", "/");

            var saveTexPath = CombinePath(textureResPath, Path.GetFileName(texPath));

            CopyTexture(texPath, saveTexPath);

            Texture releaseTex = AssetDatabase.LoadAssetAtPath<Texture>(saveTexPath);
            if (releaseTex == null)
            {
                Debug.LogErrorFormat("Texture Error! {0}", releaseTex.name);
                return false;
            }

            mat.SetTexture(propertyName, releaseTex);

            return false;
        }

        static void ExportModel(GameObject go, string rawPath, string savePath)
        {
            string modelType = GetModelType(rawPath);
            string modelName = GetModelName(rawPath);

            Renderer[] rendererArr = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rendererArr.Length; i++)
            {
                rendererArr[i].receiveShadows = false;
                rendererArr[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rendererArr[i].lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                rendererArr[i].reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }

            Animator anim = go.GetComponent<Animator>();
            if (!anim)
            {
                go.AddComponent<Animator>();
            }

            string avatarPath = ModelResPath + modelType + "/" + modelName + "/" + anim.avatar.name + ".asset";
            if (File.Exists(avatarPath))
            {
                anim.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath);
            }

            string modelPath = savePath;
            CreatePrefab(go, modelPath);
            AssetDatabase.Refresh();
        }

        public static void CopyAsset(string srcPath, string destPath)
        {
            if (srcPath == destPath)
            {
                EditorUtility.DisplayDialog("error", string.Format("{0} srcPath==destPath", srcPath), "ok");
                throw new System.IO.IOException();
            }
            UnityEngine.Object destOldAsset = AssetDatabase.LoadAssetAtPath(destPath, typeof(UnityEngine.Object)) as UnityEngine.Object;
            if (destOldAsset == null)
            {
                AssetDatabase.CopyAsset(srcPath, destPath);
            }
            else
            {
                if (File.Exists(destPath))
                {
                    File.Copy(srcPath, destPath, true);
                }
                else
                {
                    FileUtil.DeleteFileOrDirectory(destPath);
                    FileUtil.CopyFileOrDirectory(srcPath, destPath);
                }
                AssetDatabase.ImportAsset(destPath);
            }
        }

        public static void CreateAsset(UnityEngine.Object asset, string path)
        {
            var oldAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (oldAsset)
            {
                EditorUtility.CopySerialized(asset, oldAsset);
                EditorUtility.SetDirty(oldAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
            }
        }

        public static void CreateFolder(string path)
        {
            if (path.EndsWith("/"))
            {
                path = path[0..^1];
            }
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            var parentPath = Path.GetDirectoryName(path);
            CreateFolder(parentPath);
            AssetDatabase.CreateFolder(parentPath, Path.GetFileName(path));
        }

        public static void CreatePrefab(GameObject go, string path)
        {
            CreateFolder(path);
            string localPath = path + go.name + ".prefab";

            PrefabUtility.SaveAsPrefabAsset(go, localPath);
        }

        public static string CombinePath(string path_1, string path_2)
        {
            return Path.Combine(path_1, path_2).Replace('\\', '/');
        }

        public static void CopyTexture(string srcPath, string destPath)
        {
            CreateFolder(destPath.Remove(destPath.LastIndexOf("/")));
            CopyAsset(srcPath, destPath);
            var srcImporter = AssetImporter.GetAtPath(srcPath);
            var destImporter = AssetImporter.GetAtPath(destPath);
            EditorUtility.CopySerialized(srcImporter, destImporter);
            destImporter.SaveAndReimport();
        }
    }
}
