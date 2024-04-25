using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Asset
{
    public sealed class BundleRequest : LoadRequest
    {
        internal AssetBundle assetBundle { get; private set; }

        public string bundleName;

        public override int priority => 0;

        protected override void OnStart()
        {
            LoadAssetBundle();
        }

        protected override void OnUpdated()
        {

        }

        protected override void OnWaitForCompletion()
        {

        }

        public void LoadAssetBundle()
        {

            assetBundle = AssetBundle.LoadFromFile(path);
            if (assetBundle == null)
            {
                SetResult(Result.Failed, $"assetBundle == null, {bundleName}");
                return;
            }

            progress = 1;
            SetResult(Result.Success);
        }

        protected override void OnDispose()
        {

            Remove(this);
            if (assetBundle == null) return;
            assetBundle.Unload(true);
            assetBundle = null;
        }

        #region Internal

        private static readonly Queue<BundleRequest> Unused = new Queue<BundleRequest>();
        public static readonly Dictionary<string, BundleRequest> Loaded = new Dictionary<string, BundleRequest>();

        private static void Remove(BundleRequest request)
        {
            Loaded.Remove(request.bundleName);
            Unused.Enqueue(request);
        }

        internal static BundleRequest Load(string bundleName)
        {
            if (!Loaded.TryGetValue(bundleName, out var request))
            {
                request = Unused.Count > 0 ? Unused.Dequeue() : new BundleRequest();
                request.bundleName = bundleName;
                request.Reset();
                Loaded[bundleName] = request;
            }

            request.LoadAsync();
            return request;
        }

        #endregion

    }
}