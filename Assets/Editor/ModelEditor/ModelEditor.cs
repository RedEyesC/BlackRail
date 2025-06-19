
using GameEditor.Utility;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameEditor.ModelEditor

{
    public class ModelEditor
    {

        static readonly string ModelRawPath = "Assets/RawData/Model/";
        static readonly string ModelResPath = "Assets/Resources/Model/";
        static readonly string ModelDefaultShader = "BRShader/Character/Default/Default";

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

            DirectoryInfo dirInfo = new DirectoryInfo(path);
            foreach (var file in dirInfo.GetFiles())
            {
                if (file.Extension.ToLower() == ".fbx")
                {
                    string rawPath = path + "/" + file.Name;
                    string savePath = path.Replace("RawData", "Resources") + "/Anim/";
                    ExportAnim(rawPath, savePath);
                }
            }

            Debug.LogFormat("{0} Export Anim Success！！", path);
        }



        public static void ExportModelInfo(string path)
        {

            GameObject go = null;

            DirectoryInfo dirInfo = new DirectoryInfo(path);
            foreach (var file in dirInfo.GetFiles())
            {
                if (file.Extension.ToLower() == ".fbx")
                {
                    GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(path + "/" + file.Name);
                    go = GameObject.Instantiate(pref);
                    go.name = Path.GetFileNameWithoutExtension(file.Name);
                    break;
                }
            }
            string modelName = GetModelName(path);
            string avatarResPath = ModelResPath + modelName + "/Avatar/";
            ExportAvatar(go, avatarResPath);

            string meshResPath = ModelResPath + modelName + "/Mesh/";
            ExportMesh(go, meshResPath);

            string textureResPath = ModelResPath + modelName + "/Materials/";
            ExportMaterial(go, path, ModelResPath, textureResPath);

            string resPrefabPath = ModelResPath + modelName + "/";

            ExportModel(go, path, resPrefabPath);

            GameObject.DestroyImmediate(go);

            Debug.LogFormat("{0} Export Model Success！！", modelName);
        }

        public static string GetModelName(string path)
        {
            if (path.Contains(ModelRawPath))
            {
                path = path.Replace(ModelRawPath, "");
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


            CommonUtility.CreateFolder(newAvPath.Remove(newAvPath.LastIndexOf("/")));
            CommonUtility.CreateAsset(newAvatar, newAvPath);

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

            CommonUtility.CreateFolder(meshPath.Remove(meshPath.LastIndexOf("/")));
            CommonUtility.CreateAsset(newMesh, meshPath);
        }

        static void ExportMaterial(GameObject go, string rawPath, string savePath, string textureResPath)
        {
            Renderer[] rendererArr = go.GetComponentsInChildren<Renderer>();
            for (int k = 0; k < rendererArr.Length; k++)
            {
                Material[] mats = rendererArr[k].sharedMaterials;
                Material[] newMats = new Material[mats.Length];
                for (int j = 0; j < mats.Length; j++)
                {

                    Material mat;
                    mat = new Material(Shader.Find(ModelDefaultShader)) { name = mats[j].name+"_"+k, };

                    string newMatPath = textureResPath + mat.name + ".mat";
                    CommonUtility.CreateFolder(newMatPath.Remove(newMatPath.LastIndexOf("/")));
                    AssetDatabase.CreateAsset(mat, newMatPath);

                    Material newMat = AssetDatabase.LoadAssetAtPath<Material>(newMatPath);
                    newMats[j] = newMat;

                }
                rendererArr[k].sharedMaterials = newMats;
            }

            AssetDatabase.Refresh();
        }

        static void ExportModel(GameObject go, string rawPath, string savePath)
        {
            string modelName = GetModelName(rawPath);

            Renderer[] rendererArr = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rendererArr.Length; i++)
            {
                rendererArr[i].receiveShadows = false;
                rendererArr[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rendererArr[i].lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                rendererArr[i].reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            }


            go.AddComponent<AnimPlayableComponent>();

            Animator anim = go.GetComponent<Animator>();
            if (!anim)
            {
               go.AddComponent<Animator>();
            }

            string avatarPath = ModelResPath + modelName + "/" + anim.avatar.name + ".asset";
            if (File.Exists(avatarPath))
            {
                anim.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath);
            }

            string modelPath = savePath;
            CommonUtility.CreatePrefab(go, modelPath);
            AssetDatabase.Refresh();
        }

        static void ExportAnim(string rawPath, string savePath)
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(rawPath);
            foreach (Object o in objs)
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
                    CommonUtility.CreateFolder(savePath);
                    string resAnimPath = savePath + clip.name + ".anim";
                    CommonUtility.CreateAsset(newClip, resAnimPath);
                }
            }

        }
    }
}
