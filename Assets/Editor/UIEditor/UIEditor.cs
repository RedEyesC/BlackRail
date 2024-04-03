
using UnityEditor;

namespace GameEditor.UIEditor
{
    internal class UIEditor
    {
        static readonly string UIResPath = "Assets/Resources/model/";

        static void ExportUIInfo()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                ExportUIInfo(path);
            }
        }

        [MenuItem("Assets/Game Editor/导出UI定义文件", true)]
        static bool ValidExportUIInfo()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (path.Contains(UIResPath) && AssetDatabase.IsValidFolder(path))
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

        public static void ExportUIInfo(string path)
        {

            //GameObject go = null;
            //DirectoryInfo dirInfo = new DirectoryInfo(path);
            //foreach (var file in dirInfo.GetFiles())
            //{
            //    if (file.Extension.ToLower() == ".fbx")
            //    {
            //        GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(path + "/" + file.Name);
            //        go = GameObject.Instantiate(pref);
            //        break;
            //    }
            //}
            //string modelName = GetModelName(path);
            //string avatarResPath = ModelResPath + modelName + "/avatar/";
            //ExportAvatar(go, avatarResPath);

            //string meshResPath = ModelResPath + modelName + "/mesh/";
            //ExportMesh(go, meshResPath);

            //string textureResPath = ModelResPath + modelName + "/materials/";
            //ExportMaterial(go, path, ModelResPath, textureResPath);

            //string resPrefabPath = ModelResPath + modelName + "/";

            //ExportModel(go, path, resPrefabPath);

            //GameObject.DestroyImmediate(go);

            //Debug.LogFormat("{0} Export Model Success！！", modelName);
        }

    }
}
