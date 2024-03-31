using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Runtime
{
    public sealed class DownloadRequest : LoadRequest
    {
        
        public string bundleName;

        public override int priority => 0;

        // ReSharper disable once UnusedAutoPropertyAccessor.Global

        protected override void OnStart()
        {
            //_retryTimes = 0;
            //var bundle = request.info;
            //var url = Assets.GetDownloadURL(bundle.file);
            //_savePath = Assets.GetDownloadDataPath(bundle.file);
            //_downloadAsync = Downloader.DownloadAsync(DownloadContent.Get(url, _savePath, bundle.hash, bundle.size));
        }

        protected override void OnUpdated()
        {
            //request.progress = _downloadAsync.progress;
            //if (!_downloadAsync.isDone)
            //    return;

            //if (_downloadAsync.result == DownloadRequestBase.Result.Success)
            //{
            //    request.LoadAssetBundle(_savePath);
            //    return;
            //}

            //// 网络可达才自动 Retry
            //if (Application.internetReachability != NetworkReachability.NotReachable
            //    && _retryTimes < Assets.MaxRetryTimes)
            //{
            //    _downloadAsync.Retry();
            //    _retryTimes++;
            //    return;
            //}

            //// 网络不可达的时候，如果是异步加载，可以提示用户检查网络链接。

            //request.SetResult(Request.Result.Failed, _downloadAsync.error);
        }

        protected override void OnWaitForCompletion()
        {
            
        } 

        protected override void OnDispose()
        {

        }

    }
}