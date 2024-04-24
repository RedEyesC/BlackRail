

using GameFramework.Common;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Asset
{
    public enum ResMod
    {
        Raw,
        Bundle
    }

    public interface IRecyclable
    {
        void EndRecycle();
        bool CanRecycle();
        bool IsUnused();
        void RecycleAsync();
        bool Recycling();
    }

    public class AssetManager : GameModule
    {
        private static readonly Dictionary<string, RequestQueue> _queuesMap = new Dictionary<string, RequestQueue>();
        private static readonly List<RequestQueue> _queues = new List<RequestQueue>();
        private static readonly Queue<RequestQueue> _append = new Queue<RequestQueue>();

        private static float _elapseSeconds;

        private static byte _updateMaxRequests = maxRequests;
        public static bool autoslicing { get; set; } = true;
        public static bool Working => _queues.Exists(o => o.working);

        public static bool busy =>
            autoslicing && _elapseSeconds > autoslicingTimestep;

        public static float autoslicingTimestep { get; set; } = 1f / 60f;
        public static byte maxRequests { get; set; } = 10;

        private static readonly Queue<Object> UnusedAssets = new Queue<Object>();
        private static readonly List<IRecyclable> Recyclables = new List<IRecyclable>();
        private static readonly List<IRecyclable> Progressing = new List<IRecyclable>();

        public new int priority = 8;

        public override void Start()
        {

        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            _elapseSeconds = Time.realtimeSinceStartup;

            if (_append.Count > 0)
            {
                while (_append.Count > 0)
                {
                    var item = _append.Dequeue();
                    _queues.Add(item);
                }

                _queues.Sort(Comparison);
            }

            foreach (var queue in _queues)
                if (!queue.Update())
                    break;

            ResizeIfNeed();

            if (UnusedAssets.Count > 0)
            {
                while (UnusedAssets.Count > 0)
                {
                    var item = UnusedAssets.Dequeue();
                    Resources.UnloadAsset(item);
                }

                Resources.UnloadUnusedAssets();
            }

            for (var index = 0; index < Recyclables.Count; index++)
            {
                var request = Recyclables[index];
                if (!request.CanRecycle()) continue;

                Recyclables.RemoveAt(index);
                index--;

                // 卸载的资源加载好后，可能会被再次使用
                if (!request.IsUnused()) continue;
                request.RecycleAsync();
                Progressing.Add(request);
            }

            for (var index = 0; index < Progressing.Count; index++)
            {
                var request = Progressing[index];
                if (request.Recycling()) continue;
                Progressing.RemoveAt(index);
                index--;
                if (request.CanRecycle() && request.IsUnused()) request.EndRecycle();
                if (busy) return;
            }

        }

        #region scheduler

        private static int Comparison(RequestQueue x, RequestQueue y)
        {
            return x.priority.CompareTo(y.priority);
        }

        private static void ResizeIfNeed()
        {
            if (_updateMaxRequests == maxRequests) return;

            foreach (var queue in _queues) queue.maxRequests = maxRequests;

            _updateMaxRequests = maxRequests;
        }

        public static void Enqueue(Request request)
        {
            var key = request.GetType().Name;
            if (!_queuesMap.TryGetValue(key, out var queue))
            {
                queue = new RequestQueue { key = key, maxRequests = maxRequests, priority = request.priority };
                _queuesMap.Add(key, queue);
                _append.Enqueue(queue);
            }

            queue.Enqueue(request);
        }

        public override void Destroy()
        {

        }


        public void Restart()
        {

        }

        #endregion


        #region Request

        public static AssetRequest LoadAssetAsync(string bundleName, string assetName, System.Action<Request> callback = null)
        {
            return AssetRequest.Load(bundleName, assetName, false, callback);
        }

        public static AssetRequest LoadAllAssetAsync(string bundleName, System.Action<Request> callback = null)
        {
            return AssetRequest.Load(bundleName, null, true, callback);
        }

        public static void UnLoadAssetAsync(AssetRequest req)
        {
            req.Release();
        }

        #endregion


        #region Reycle

        public static void UnloadAsset(Object asset)
        {
            UnusedAssets.Enqueue(asset);
        }


        public static void RecycleAsync(IRecyclable recyclable)
        {
            // 防止重复回收
            if (Recyclables.Contains(recyclable) || Progressing.Contains(recyclable))
                return;
            Recyclables.Add(recyclable);
        }

        #endregion


        #region Asset

        public static Object GetAssetObj(string bundleName, string assetName , bool isAll = false)
        {
            return AssetRequest.Get<Object>(bundleName, assetName,isAll);
        }


        public static T GetAssetObjWithType<T>(string bundleName, string assetName,bool isAll = false) where T : Object
        {
            return AssetRequest.Get<T>(bundleName, assetName, isAll);
        }

        #endregion


        #region Depend

        //TODO
        public static string[] GetBundleDepend(string bundleName)
        {
            return null;
        }

        #endregion

    }

}