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

        public Object asset { get; set; }
        public Object[] assets { get; set; }
        public bool isAll { get; private set; }
        public override int priority => 1;

        private enum Step
        {
            LoadDependencies,
            LoadAsset
        }

        private Dependencies _dependencies;
        private BundleRequest _loadBundleRequest;
        private Step _step;


        protected override void OnStart()
        {
            _dependencies = Dependencies.LoadAsync(bundleName);
            _step = Step.LoadDependencies;
        }

        protected override void OnWaitForCompletion()
        {
            _dependencies.WaitForCompletion();
            if (result == Request.Result.Failed) return;
            //  特殊处理，防止异步转同步卡顿。
            if (_loadBundleRequest == null)
                LoadAsset();
            else
                SetResult();
        }

        protected override void OnUpdated()
        {
            if (isDone) return;
            switch (_step)
            {
                case Step.LoadDependencies:
                    _dependencies.Update();
                    progress = _dependencies.progress * 0.5f;
                    if (!_dependencies.isDone) return;
                    LoadAssetAsync();
                    break;

                case Step.LoadAsset:
                    progress = 0.5f + _loadBundleRequest.loadProgress * 0.5f;
                    if (!_loadBundleRequest.loadIsDone) return;
                    SetResult();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LoadAsset()
        {
            if (!_dependencies.CheckResult(this, out var bundleRequest))
                return;

            if (isAll)
            {
                assets = bundleRequest.LoadAllAssets();
                if (assets == null)
                {
                    SetResult(Request.Result.Failed, "assets == null");
                    return;
                }
            }
            else
            {
                asset = bundleRequest.LoadAsset(assetName);
                if (asset == null)
                {
                    SetResult(Request.Result.Failed, "asset == null");
                    return;
                }
            }

            SetResult(Request.Result.Success);
        }


        private void LoadAssetAsync()
        {
            if (!_dependencies.CheckResult(this, out var bundleRequest))
                return;

            _loadBundleRequest = bundleRequest;

            if (isAll)
            {
                bundleRequest.LoadAllAssetsAsync();
            }
            else
            {
                bundleRequest.LoadAssetAsync(assetName);
            }

            _step = Step.LoadAsset;
        }


        private void SetResult()
        {
            if (isAll)
            {
                assets = _loadBundleRequest.loadAssetObjs;
                if (assets == null)
                {
                    SetResult(Request.Result.Failed, "assets == null");
                    return;
                }
            }
            else
            {
                asset = _loadBundleRequest.loadAssetObjs[0];
                if (asset == null)
                {
                    SetResult(Request.Result.Failed, "asset == null");
                    return;
                }
            }

            SetResult(Request.Result.Success);
        }


        protected override void OnDispose()
        {
            Remove(this);

            _dependencies.Release();
            _loadBundleRequest = null;

            if (isAll)
            {
                if (assets != null)
                    foreach (var o in assets)
                        if (!(o is GameObject))
                            AssetManager.UnloadAsset(o);
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

        internal static T Get<T>(string bundleName, string assetName) where T : Object
        {
            var path = $"{bundleName}/{assetName}";
            if (!Loaded.TryGetValue(path, out var request)) return null;
            return request.asset as T;
        }

        internal static T[] GetAll<T>(string bundleName, string assetName) where T : Object
        {
            var path = $"{bundleName}/{assetName}";
            if (!Loaded.TryGetValue(path, out var request)) return null;
            return request.assets as T[];
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