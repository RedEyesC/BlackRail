using System;
using UnityEngine;

namespace GameFramework.Runtime
{
    public interface IAssetHandler
    {
        void OnStart(AssetRequest request);
        void Update(AssetRequest request);
        void Dispose(AssetRequest request);
        void WaitForCompletion(AssetRequest request);
    }

    public struct RuntimeAssetHandler : IAssetHandler
    {
        private enum Step
        {
            LoadDependencies,
            LoadAsset
        }

        private Dependencies _dependencies;
        private AssetBundleRequest _loadAssetAsync;
        private Step _step;

        public void OnStart(AssetRequest request)
        {
            _dependencies = Dependencies.LoadAsync(request.bundleName);
            _step = Step.LoadDependencies;
        }

        public void Update(AssetRequest request)
        {
            if (request.isDone) return;
            switch (_step)
            {
                case Step.LoadDependencies:
                    _dependencies.Update();
                    request.progress = _dependencies.progress * 0.5f;
                    if (!_dependencies.isDone) return;
                    LoadAssetAsync(request);
                    break;

                case Step.LoadAsset:
                    request.progress = 0.5f + _loadAssetAsync.progress * 0.5f;
                    if (!_loadAssetAsync.isDone) return;
                    SetResult(request);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LoadAsset(AssetRequest request)
        {
            if (!_dependencies.CheckResult(request, out var assetBundle))
                return;

            if (request.isAll)
            {
                request.assets = assetBundle.LoadAllAssets();
                if (request.assets == null)
                {
                    request.SetResult(Request.Result.Failed, "assets == null");
                    return;
                }
            }
            else
            {
                request.asset = assetBundle.LoadAsset(request.assetName);
                if (request.asset == null)
                {
                    request.SetResult(Request.Result.Failed, "asset == null");
                    return;
                }
            }

            request.SetResult(Request.Result.Success);
        }

        private void LoadAssetAsync(AssetRequest request)
        {
            if (!_dependencies.CheckResult(request, out var assetBundle))
                return;

            _loadAssetAsync = request.isAll
                ? assetBundle.LoadAllAssetsAsync()
                : assetBundle.LoadAssetAsync(request.assetName);
            _step = Step.LoadAsset;
        }

        private void SetResult(AssetRequest request)
        {
            if (request.isAll)
            {
                request.assets = _loadAssetAsync.allAssets;
                if (request.assets == null)
                {
                    request.SetResult(Request.Result.Failed, "assets == null");
                    return;
                }
            }
            else
            {
                request.asset = _loadAssetAsync.asset;
                if (request.asset == null)
                {
                    request.SetResult(Request.Result.Failed, "asset == null");
                    return;
                }
            }

            request.SetResult(Request.Result.Success);
        }

        public void Dispose(AssetRequest request)
        {
            _dependencies.Release();
            _loadAssetAsync = null;
        }

        public void WaitForCompletion(AssetRequest request)
        {
            _dependencies.WaitForCompletion();
            if (request.result == Request.Result.Failed) return;
            //  特殊处理，防止异步转同步卡顿。
            if (_loadAssetAsync == null)
                LoadAsset(request);
            else
                SetResult(request);
        }

        public static IAssetHandler CreateInstance()
        {
            return new RuntimeAssetHandler();
        }
    }
}