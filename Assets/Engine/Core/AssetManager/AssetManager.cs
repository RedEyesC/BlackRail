using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

namespace GameFramework.Runtime
{
    public class AssetManager : Singleton<AssetManager>
    {
        private bool mIsResourceMode = false;
        private Dictionary<string, AssetBundleInfo> mBundleInfoMap = new Dictionary<string, AssetBundleInfo>();

        private int mAssetReqID = 0;
        private Dictionary<int, AssetRequest> mAssetRequestMap = new Dictionary<int, AssetRequest>();
        private Queue<AssetRequest> mAssetRequestCache = new Queue<AssetRequest>();

        private Queue<int> mFinishRequestQueue = new Queue<int>();

        private Dictionary<string, List<string>> mDependencyMap = new Dictionary<string, List<string>>();

        private int overTime = 2;
        private LinkedListNode<AssetTask> mCurTaskNode = null;
        private LinkedList<AssetTask> mTaskList = new LinkedList<AssetTask>();
        private Stack<LinkedListNode<AssetTask>> mTaskNodeCache = new Stack<LinkedListNode<AssetTask>>();

        private Stopwatch mWatch = new Stopwatch();


        public static AssetManager GetInstance()
        {
            return Instance;
        }

        public void Start()
        {
            mIsResourceMode = true;
        }

        public void Update()
        {
            mWatch.Reset();
            mWatch.Start();
            mCurTaskNode = mTaskList.First;
            LinkedListNode<AssetTask> tmpNode = null;
            // 这个Update+while循环就是一直在等待 链头(First)task 完成
            // 加IsOverTime是为了防止某些资源加载太久，while循环卡住Update
            // 同类型的任务操作可以 在同一帧的Update同步进行
            // 每次Update都会从 链头 开始检查，task是否完成
            int taskMask = 0;
            while ((mCurTaskNode != null)
                 && (!IsOverTime())
                 && ((taskMask = CanExcute(mCurTaskNode.Value, mCurTaskNode.Previous == null, taskMask)) > 0))
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
            //UnityEngine.Debug.LogFormat("UpdateTask {0} {1}", AssetTask.CurTaskCount, mTaskList.Count);
            mWatch.Stop();
        }

        public void Stop(bool isRestart = false)
        {
            mAssetRequestMap.Clear();
            mFinishRequestQueue.Clear();
            mDependencyMap.Clear();
      
            foreach (var item in mBundleInfoMap)
            {
                if (item.Value.IsLoaded && (!isRestart))
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
                    info.ResetDownloadState();
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
            return mWatch.ElapsedMilliseconds > this.overTime;
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
        public int LoadAssetAsync(string bundleName, string assetName, bool needDownwload, System.Action<int, bool> callback = null)
        {
            AssetBundleInfo bundleInfo = GetBundleInfo(bundleName);
            AssetRequest req = CreateAssetRequest();
            req.InitLoadAssetRequest(bundleInfo, assetName, needDownwload);
            req.SetRequestFinishCallBack(callback);
            req.ProcessRequest();
            return req.RequestID;
        }

        public int UnLoadAssetAsync(string bundleName, string assetName, System.Action<int, bool> callback = null)
        {
            AssetBundleInfo bundleInfo = GetBundleInfo(bundleName);
            AssetRequest req = CreateAssetRequest();
            req.InitUnLoadAssetRequest(bundleInfo, assetName);
            req.SetRequestFinishCallBack(callback);
            req.ProcessRequest();
            return req.RequestID;
        }


        public void UnLoadUnuseAsset()
        {
            UnloadUnuseAssetTask task = new UnloadUnuseAssetTask();
            Instance.AddTask(task);
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
            if (mBundleInfoMap.TryGetValue(bundleName, out info))
            {
                return info;
            }
            else
            {
                info = new AssetBundleInfo(bundleName, mIsResourceMode);
                mBundleInfoMap.Add(bundleName, info);

                List<string> depList = null;
                mDependencyMap.TryGetValue(bundleName, out depList);
                info.InitDependency(depList);
            }
            return info;
        }
        #endregion

        #region Asset Request
        private AssetRequest CreateAssetRequest()
        {
            AssetRequest req = null;
            if (mAssetRequestCache.Count > 0)
                req = mAssetRequestCache.Dequeue();
            else
                req = new AssetRequest();

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

        private int CanExcute(AssetTask task, bool firstTask, int taskMask)
        {
            if (firstTask)
                return task.TaskType;

            int mask = taskMask & task.BanSelfRunTaskMask;
            if (mask > 0)
            {
                return -1;
            }
            else
            {
                return task.TaskType | taskMask;
            }
        }
        #endregion

    }

}