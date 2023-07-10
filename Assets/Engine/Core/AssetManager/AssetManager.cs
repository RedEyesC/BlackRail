using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace GameFramework.Runtime
{
    public delegate void RequestCallBack(int reqID, bool isSuccess);


    public class AssetManager : GameModule
    {


        private int _AssetReqID = 0;
        private Dictionary<int, AssetRequest> _AssetRequestMap = new Dictionary<int, AssetRequest>();
        private Queue<AssetRequest> _AssetRequestCache = new Queue<AssetRequest>();

        private Queue<int> _FinishRequestQueue = new Queue<int>();

        private Dictionary<string, AssetInfo> _AssetInfoMap = new Dictionary<string, AssetInfo>();

        private LinkedListNode<AssetTask> _CurTaskNode = null;
        private LinkedList<AssetTask> _TaskList = new LinkedList<AssetTask>();
        private Stack<LinkedListNode<AssetTask>> _TaskNodeCache = new Stack<LinkedListNode<AssetTask>>();


        private struct LoadAssetInfo
        {


        }

        public override void Start()
        {

        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {

            _CurTaskNode = _TaskList.First;
            LinkedListNode<AssetTask> tmpNode = null;
            while ((_CurTaskNode != null))

            {
                if (_CurTaskNode.Value.Update())
                {
                    _CurTaskNode.Value.Reset();
                    tmpNode = _CurTaskNode;
                    _CurTaskNode = _CurTaskNode.Next;

                    _TaskList.Remove(tmpNode);
                    tmpNode.Value = null;
                    //把使用完的节点压入缓存队列，等待下次复用
                    _TaskNodeCache.Push(tmpNode);
                }
                else
                {
                    _CurTaskNode = _CurTaskNode.Next;
                }
            }
        }

        public override void Destroy()
        {
            _AssetRequestMap.Clear();
            _FinishRequestQueue.Clear();

            _AssetInfoMap.Clear();

            _AssetReqID = 0;
            _AssetRequestCache.Clear();


            _CurTaskNode = null;
            _TaskList.Clear();
            _TaskNodeCache.Clear();


        }


        public void Restart()
        {
            _AssetRequestMap.Clear();
            _FinishRequestQueue.Clear();


            _CurTaskNode = null;
            _TaskList.Clear();
            _TaskNodeCache.Clear();

        }


        #region Asset Task

        public void AddTask(AssetTask task)
        {
            if (_TaskNodeCache.Count > 0)
            {
                LinkedListNode<AssetTask> node = _TaskNodeCache.Pop();
                node.Value = task;
                _TaskList.AddLast(node);
            }
            else
            {
                _TaskList.AddLast(task);
            }
        }

        public int GetTaskNum()
        {
            return _TaskList.Count;
        }

        public void SetMaxTaskNum(int n)
        {
            AssetTask.SetMaxTaskNum(n);
        }
        #endregion



        #region Get Asset
        public AssetInfo GetAssetInfo(string assetName)
        {
            AssetInfo info = null;
            if (_AssetInfoMap.TryGetValue(assetName, out info))
            {
                return info;
            }
            else
            {
                info = new AssetInfo(assetName);
                _AssetInfoMap.Add(assetName, info);

            }
            return info;
        }

        public UnityEngine.Object GetAssetObj(string assetName)
        {
            AssetInfo info = GetAssetInfo(assetName);
            if (info != null)
            {
                return info.GetAssetObj();
            }
            return null;
        }

        public T GetAssetObjWithType<T>(string assetName) where T : class
        {
            AssetInfo info = GetAssetInfo(assetName);
            if (info != null)
            {
                return info.GetAssetObjWithType<T>();
            }
            return null;
        }
        #endregion




        #region Request Load Asset
        public int LoadAssetAsync(string assetName, RequestCallBack callback = null)
        {
            AssetRequest req = CreateAssetRequest(assetName, callback, AssetRequestType.LoadOne);
            req.ProcessRequest();
            return req.RequestID;
        }

        public int LoadAllAssetAsync(string assetName, RequestCallBack callback = null)
        {
            AssetRequest req = CreateAssetRequest(assetName, callback, AssetRequestType.LoadAll);
            req.ProcessRequest();
            return req.RequestID;
        }


        public int UnLoadAssetAsync(string assetName, RequestCallBack callback = null)
        {
            AssetRequest req = CreateAssetRequest(assetName, callback, AssetRequestType.UnloadOne);
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

        #endregion

        #region Asset Request
        private AssetRequest GetAssetRequest(int reqID)
        {
            AssetRequest req = null;
            _AssetRequestMap.TryGetValue(reqID, out req);
            return req;
        }

        private AssetRequest CreateAssetRequest(string assetName, RequestCallBack callback, AssetRequestType type)
        {

            AssetInfo info = GetAssetInfo(assetName);


            AssetRequest req = null;
            if (_AssetRequestCache.Count > 0)
                req = _AssetRequestCache.Dequeue();
            else
                req = new AssetRequest(info, type, OnRequestFinish);

            req.RequestID = ++_AssetReqID;
            req.SetTaskFinishCallBack(callback);

            _AssetRequestMap.Add(req.RequestID, req);

            return req;
        }

        private void OnRequestFinish(AssetRequest req)
        {
            if (req.IsCancel)
                FreeLoadRequest(req);
            else
                _FinishRequestQueue.Enqueue(req.RequestID);
        }

        private void FreeLoadRequest(AssetRequest req)
        {
            if (_AssetRequestMap.ContainsKey(req.RequestID))
            {
                _AssetRequestMap.Remove(req.RequestID);
            }

            req.Reset();

            _AssetRequestCache.Enqueue(req);
        }
        #endregion

    }

}