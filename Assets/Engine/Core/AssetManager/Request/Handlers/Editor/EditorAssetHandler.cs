using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Object = UnityEngine.Object;

namespace GameFramework.Asset
{
    public struct EditorAssetHandler : IAssetHandler
    {

        public void OnStart(AssetRequest request)
        {

            Object[] loadAssetObjs = LoadAssetByRes(request);

            if (request.isAll)
            {
                Dictionary<string, Object> dict = new Dictionary<string, Object>();
                foreach (Object obj in loadAssetObjs)
                {
                    dict.Add(obj.name, obj);
                }
                request.assets = dict;

                if (request.assets == null)
                {
                    request.SetResult(Request.Result.Failed, "assets == null");
                    return;
                }
            }
            else
            {
                request.asset = loadAssetObjs[0];
                if (request.asset == null)
                {
                    request.SetResult(Request.Result.Failed, "asset == null");
                    return;
                }
            }

            request.SetResult(Request.Result.Success);
        }

        public void Update(AssetRequest request)
        {

        }

        private void SetResult(AssetRequest request)
        {

        }

        public void Dispose(AssetRequest request)
        {

        }

        public void WaitForCompletion(AssetRequest request)
        {

        }


        public static IAssetHandler CreateInstance()
        {
            return new EditorAssetHandler();
        }


        internal Object[] LoadAssetByRes(AssetRequest request)
        {
            string bundleName = request.bundleName;
            string assetName = request.assetName;

            string filePath = "Assets/Asset/" + bundleName + "/";

            List<Object> objList = new List<Object>();
            List<string> fileList = GetFileList(filePath, assetName);


            for (int i = 0; i < fileList.Count; i++)
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(filePath + fileList[i]);
                if (obj != null)
                    objList.Add(obj);
            }

            return objList.ToArray();
        }


        private List<string> GetFileList(string bundleName, string asstName = null)
        {

            List<string> fileList = new List<string>();
            DirectoryInfo dirInfo = new DirectoryInfo(bundleName);
            foreach (var item in dirInfo.GetFiles())
            {
                if (item.Extension == ".meta")
                {
                    continue;
                }


                string name = Path.GetFileNameWithoutExtension(item.Name);
                if (asstName != null)
                {
                    if (name == asstName)
                    {
                        fileList.Add(item.Name);
                        break;
                    }
                }
                else
                {
                    fileList.Add(item.Name);
                }
            }

            return fileList;
        }
    }
}