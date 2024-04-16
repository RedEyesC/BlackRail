using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Runtime
{
    public class AssetRequest : LoadRequest
    {

        private static readonly Queue<AssetRequest> Unused = new Queue<AssetRequest>();
        internal static readonly Dictionary<string, AssetRequest> Loaded = new Dictionary<string, AssetRequest>();

        public string bundleName;
        public string assetName;

        public IAssetHandler handler { get;} = CreateHandler();

        public Object asset { get; set; }
        public Dictionary<string,Object> assets { get; set; }
        public bool isAll { get; private set; }
        public override int priority => 1;

        public static Func<IAssetHandler> CreateHandler { get; set; } = EditorAssetHandler.CreateInstance;
 
        protected override void OnStart()
        {
            handler.OnStart(this);
        }

        protected override void OnWaitForCompletion()
        {
            handler.WaitForCompletion(this);
        }

        protected override void OnUpdated()
        {
            handler.Update(this);
        }

        protected override void OnDispose()
        {
            Remove(this);
            handler.Dispose(this);

            if (isAll)
            {
                if (assets != null)
                    foreach (var o in assets)
                        if (!(o.Value is GameObject))
                            AssetManager.UnloadAsset(o.Value);
            }
            else
            {
                if (asset != null && !(asset is GameObject)) AssetManager.UnloadAsset(asset);
            }

            asset = null;
            assets = null;
            isAll = false;
        }

        private static void Remove(AssetRequest request)
        {
            Loaded.Remove(request.path);
            Unused.Enqueue(request);
        }

        internal static T Get<T>(string bundleName, string assetName,bool isAll) where T : Object
        {
            if(!isAll)
            {
                var path = $"{bundleName}/{assetName}";
                if (!Loaded.TryGetValue(path, out var request)) return null;
                return request.asset as T;
            }
            else
            {
                var path = $"{bundleName}/";
                if (!Loaded.TryGetValue(path, out var request)) return null;
                return request.assets[assetName] as T;
            }

        }

        internal static AssetRequest Load(string bundleName, string assetName, bool isAll, System.Action<Request> callback)
        {

            var path = $"{bundleName}/{assetName}";
            if (!Loaded.TryGetValue(path, out var request))
            {
                request = Unused.Count > 0 ? Unused.Dequeue() : new AssetRequest();
                request.Reset();
                request.isAll = isAll;
                request.path = path;
                request.bundleName = bundleName;
                request.assetName = assetName;
                request.completed = callback;
                Loaded[path] = request;
            }

            request.LoadAsync();
            return request;
        }

    }
}