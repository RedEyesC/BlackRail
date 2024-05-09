using System.Collections.Generic;
using System.IO;
using UnityEngine;
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

            string resPath = bundleName.Remove(bundleName.LastIndexOf(".")) + "/";

            List<Object> objList = new List<Object>();
            List<string> fileList = GetFileList(bundleName, assetName);


            for (int i = 0; i < fileList.Count; i++)
            {
                Object obj = Resources.Load(resPath + fileList[i]);
                if (obj != null)
                    objList.Add(obj);
            }

            return objList.ToArray();
        }

       
        private List<string> GetFileList(string bundleName, string asstName = null)
        {
            string resPath = bundleName.Remove(bundleName.LastIndexOf(".")) + "/";
            string filePath = "Assets/Resources/" + resPath;

            List<string> fileList = new List<string>();
            DirectoryInfo dirInfo = new DirectoryInfo(filePath);
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
                        fileList.Add(name);
                        break;
                    }
                }
                else
                {
                    fileList.Add(name);
                }
            }

            return fileList;
        }
    }
}