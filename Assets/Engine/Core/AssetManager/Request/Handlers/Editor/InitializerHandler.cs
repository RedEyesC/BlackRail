using GameFramework.Asset;
using UnityEngine;

namespace xasset.editor
{
    public static class Initializer
    {
        //负责在editor下初始化资源加载
        [RuntimeInitializeOnLoadMethod]
        private static void RuntimeInitializeOnLoad()
        {
            AssetRequest.CreateHandler = EditorAssetHandler.CreateInstance;
            SceneRequest.CreateHandler = EditorSceneHandler.CreateInstance;
        }
    }
}