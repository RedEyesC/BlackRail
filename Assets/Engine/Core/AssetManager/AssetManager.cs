using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

namespace GameFramework.Runtime
{
    public delegate void RequestCallBack(int reqID, bool isSuccess);

    public class AssetManager : GameModule
    {
        private Dictionary<string, AssetBundleInfo> mBundleInfoMap = new Dictionary<string, AssetBundleInfo>();

        private int mAssetReqID = 0;
        private Dictionary<int, AssetRequest> mAssetRequestMap = new Dictionary<int, AssetRequest>();
        private Queue<AssetRequest> mAssetRequestCache = new Queue<AssetRequest>();

        private Queue<int> mFinishRequestQueue = new Queue<int>();

        private Dictionary<string, List<string>> mDependencyMap = new Dictionary<string, List<string>>();

        private int overTime = 5;
        private LinkedListNode<AssetTask> mCurTaskNode = null;
        private LinkedList<AssetTask> mTaskList = new LinkedList<AssetTask>();
        private Stack<LinkedListNode<AssetTask>> mTaskNodeCache = new Stack<LinkedListNode<AssetTask>>();

        private Stopwatch mWatch = new Stopwatch();


        public override void Start()
        {
          
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            mWatch.Reset();
            mWatch.Start();
            mCurTaskNode = mTaskList.First;
            LinkedListNode<AssetTask> tmpNode = null;
            while ((mCurTaskNode != null)
                 && (!IsOverTime()))
            {
                if (mCurTaskNode.Value.Update())
                {
                    mCurTaskNode.Value.Reset();
                    tmpNode = mCurTaskNode;
                    mCurTaskNode = mCurTaskNode.Next;
                    mTaskList.Remove(tmpNode);
                    tmpNode.Value = null;
                    //把使用完的节点压入缓存队列，等待下次复用
                    mTaskNodeCache.Push(tmpNode);
                }
                else
                {
                    mCurTaskNode = mCurTaskNode.Next;
                }
            }
            mWatch.Stop();
        }

        public  override void Destroy()
        {
            mAssetRequestMap.Clear();
            mFinishRequestQueue.Clear();
            mDependencyMap.Clear();
      
            foreach (var item in mBundleInfoMap)
            {
                if (item.Value.IsLoaded)
                {
                    item.Value.UnloadSelf();
                }
            }

            mBundleInfoMap.Clear();


            mAssetReqID = 0;
            mAssetRequestCache.Clear();


            mCurTaskNode = null;
            mTaskList.Clear();
            mTaskNodeCache.Clear();
     

        }

        public void AddDependency(string bundleName, List<string> depList)
        {
            if (!mDependencyMap.ContainsKey(bundleName))
            {
                mDependencyMap.Add(bundleName, depList);
            }
        }

        public void ResetBundleDepdency()
        {
            foreach (var item in mDependencyMap)
            {
                AssetBundleInfo info = GetBundleInfo(item.Key);
                if (info != null)
                {
                    info.ClearDependency();
                    info.InitDependency(item.Value);
                }
            }
        }

        public void Restart()
        {
            mAssetRequestMap.Clear();
            mFinishRequestQueue.Clear();
            mDependencyMap.Clear();

            foreach (var item in mBundleInfoMap)
            {
                if (item.Value.IsLoaded)
                    item.Value.UnloadSelf();
            }

            mBundleInfoMap.Clear();

            mCurTaskNode = null;
            mTaskList.Clear();
            mTaskNodeCache.Clear();

        }


        #region Asset Task

        private bool IsOverTime()
        {
            return mWatch.ElapsedMilliseconds > overTime;
        }

        public void AddTask(AssetTask task)
        {
            if (mTaskNodeCache.Count > 0)
            {
                LinkedListNode<AssetTask> node = mTaskNodeCache.Pop();
                node.Value = task;
                mTaskList.AddLast(node);
            }
            else
            {
                mTaskList.AddLast(task);
            }
        }

        public int GetTaskNum()
        {
            return mTaskList.Count;
        }

        public void SetMaxTaskNum(int n)
        {
            AssetTask.SetMaxTaskNum(n);
        }
        #endregion

        #region Request Load Asset
        public int LoadAssetAsync(string bundleName, string assetName, RequestCallBack callback = null)
        {
            AssetBundleInfo bundleInfo = GetBundleInfo(bundleName);
            AssetRequest req = CreateAssetRequest(bundleInfo, assetName, AssetRequestType.LoadOne,callback);
            req.ProcessRequest();
            return req.RequestID;
        }

        public int LoadAllAssetAsync(string bundleName, string assetName,RequestCallBack callback = null)
        {
            AssetBundleInfo bundleInfo = GetBundleInfo(bundleName);
            AssetRequest req = CreateAssetRequest(bundleInfo, assetName, AssetRequestType.LoadAll, callback);
            req.ProcessRequest();
            return req.RequestID;
        }


        public int UnLoadAssetAsync(string bundleName, string assetName, RequestCallBack callback = null)
        {
            AssetBundleInfo bundleInfo = GetBundleInfo(bundleName);
            AssetRequest req = CreateAssetRequest(bundleInfo, assetName, AssetRequestType.UnloadOne, callback);
            req.ProcessRequest();
            return req.RequestID;
        }

        public void UnLoad(int reqID)
        {
            AssetRequest req = GetAssetRequest(reqID);
            if (req != null)
            {
                req.IsCancel = true;
                if (!req.IsRunning)
                    FreeLoadRequest(req);
            }
        }

        public void UnLoadUnuseAsset()
        {
            UnloadUnuseAssetTask task = new UnloadUnuseAssetTask();
            AddTask(task);
        }


        public void DelAssetRef(string bundleName, string assetName)
        {
            AssetBundleInfo bundlInfo = null;
            if (mBundleInfoMap.TryGetValue(bundleName, out bundlInfo))
            {
                bundlInfo.DelAssetRef(assetName);
            }
        }

        #endregion

        #region Get Bundle Asset
        public int GetBundleNum()
        {
            return AssetBundleInfo.AssetBundleNum;
        }

        public AssetBundleInfo GetBundleInfo(string bundleName)
        {
            AssetBundleInfo info = null;
            if (!mBundleInfoMap.TryGetValue(bundleName, out info))
            {
                info = new AssetBundleInfo(bundleName);
                mBundleInfoMap.Add(bundleName, info);

                List<string> depList = null;
                mDependencyMap.TryGetValue(bundleName, out depList);
                info.InitDependency(depList);
              
            }
   
            return info;
        }
        #endregion

        #region Asset Request
        private AssetRequest GetAssetRequest(int reqID)
        {
            AssetRequest req = null;
            mAssetRequestMap.TryGetValue(reqID, out req);
            return req;
        }

        private AssetRequest CreateAssetRequest(AssetBundleInfo bundleInfo, string assetName, AssetRequestType type,RequestCallBack  callback = null)
        {
            AssetRequest req = null;
            if (mAssetRequestCache.Count > 0)
                req = mAssetRequestCache.Dequeue();
            else
                req = new AssetRequest(bundleInfo,assetName,type);

            req.SetRequestFinishCallBack(callback);
            req.RequestID = ++mAssetReqID;
            req.SetTaskFinishCallBack(OnRequestFinish);
            mAssetRequestMap.Add(req.RequestID, req);

            return req;
        }

        private void OnRequestFinish(AssetRequest req)
        {
            if (req.IsCancel)
                FreeLoadRequest(req);
            else
                mFinishRequestQueue.Enqueue(req.RequestID);
        }

        private void FreeLoadRequest(AssetRequest req)
        {
            if (mAssetRequestMap.ContainsKey(req.RequestID))
            {
                mAssetRequestMap.Remove(req.RequestID);
            }

            req.Reset();

            mAssetRequestCache.Enqueue(req);
        }
        #endregion

    }

}