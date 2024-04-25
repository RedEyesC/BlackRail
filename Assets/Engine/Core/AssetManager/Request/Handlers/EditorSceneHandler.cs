using System.Security.Cryptography;
using UnityEditor.Build.Content;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFramework.Asset
{
    public struct EditorSceneHandler : ISceneHandler
    {
        public void OnStart(SceneRequest request)
        {
        }

        public void Update(SceneRequest request)
        {
        }

        public void Release(SceneRequest request)
        {
        }

        public AsyncOperation LoadSceneAsync(SceneRequest request)
        {
            string path = "Assets/Resources/" + request.bundName.Remove(request.bundName.LastIndexOf(".")) + "/" + request.assetName+".unity";
            var parameters = new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive };
            return EditorSceneManager.LoadSceneAsyncInPlayMode(path, parameters);
        }

        public bool IsReady(SceneRequest request1)
        {
            return true;
        }

        public void WaitForCompletion(SceneRequest request)
        {
            string path = "Assets/Resources/" + request.bundName.Remove(request.bundName.LastIndexOf(".")) + "/" + request.assetName + ".unity";
            var parameters = new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive };
            EditorSceneManager.LoadSceneAsyncInPlayMode(path, parameters);
            request.SetResult(Request.Result.Success);
        }

        public float progressRate => 1;

        public static ISceneHandler CreateInstance()
        {
            return new EditorSceneHandler();
        }
    }
}