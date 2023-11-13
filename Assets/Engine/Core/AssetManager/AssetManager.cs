using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace GameFramework.Runtime
{
    public delegate void RequestCallBack(int reqID, bool isSuccess);


    public class AssetManager : GameModule
    {


        private int _assetReqID = 0;
        private Dictionary<int, AssetRequest> _assetRequestMap = new Dictionary<int, AssetRequest>();
        private Queue<AssetRequest> _assetRequestCache = new Queue<AssetRequest>();

        private Queue<int> _finishRequestQueue = new Queue<int>();

        private Dictionary<string, AssetInfo> _assetInfoMap = new Dictionary<string, AssetInfo>();

        private LinkedListNode<AssetTask> _curTaskNode = null;
        private LinkedList<AssetTask> _taskList = new LinkedList<AssetTask>();
        private Stack<LinkedListNode<AssetTask>> _taskNodeCache = new Stack<LinkedListNode<AssetTask>>();


        private struct LoadAssetInfo
        {


        }

        public override void Start()
        {

        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {

            _curTaskNode = _taskList.First;
            LinkedListNode<AssetTask> tmpNode = null;
            while ((_curTaskNode != null))

            {
                if (_curTaskNode.Value.Update())
                {
                    _curTaskNode.Value.Reset();
                    tmpNode = _curTaskNode;
                    _curTaskNode = _curTaskNode.Next;

                    _taskList.Remove(tmpNode);
                    tmpNode.Value = null;
                    //把使用完的节点压入缓存队列，等待下次复用
                    _taskNodeCache.Push(tmpNode);
                }
                else
                {
                    _curTaskNode = _curTaskNode.Next;
                }
            }
        }

        public override void Destroy()
        {
            _assetRequestMap.Clear();
            _finishRequestQueue.Clear();

            _assetInfoMap.Clear();

            _assetReqID = 0;
            _assetRequestCache.Clear();


            _curTaskNode = null;
            _taskList.Clear();
            _taskNodeCache.Clear();


        }


        public void Restart()
        {
            _assetRequestMap.Clear();
            _finishRequestQueue.Clear();


            _curTaskNode = null;
            _taskList.Clear();
            _taskNodeCache.Clear();

        }


        #region Asset Task

        public void AddTask(AssetTask task)
        {
            if (_taskNodeCache.Count > 0)
            {
                LinkedListNode<AssetTask> node = _taskNodeCache.Pop();
                node.Value = task;
                _taskList.AddLast(node);
            }
            else
            {
                _taskList.AddLast(task);
            }
        }

        public int GetTaskNum()
        {
            return _taskList.Count;
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
            if (_assetInfoMap.TryGetValue(assetName, out info))
            {
                return info;
            }
            else
            {
                info = new AssetInfo(assetName);
                _assetInfoMap.Add(assetName, info);

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

        public int LoadSceneAsync(string assetName, RequestCallBack callback = null)
        {
            AssetRequest req = CreateAssetRequest(assetName, callback, AssetRequestType.LoadScene);
            req.ProcessRequest();
            return req.requestID;
        }

        public int UnLoadSceneAsync(string assetName, RequestCallBack callback = null)
        {
            AssetRequest req = CreateAssetRequest(assetName, callback, AssetRequestType.UnloadScene);
            req.ProcessRequest();
            return req.requestID;
        }


        public int LoadAssetAsync(string assetName, RequestCallBack callback = null)
        {
            AssetRequest req = CreateAssetRequest(assetName, callback, AssetRequestType.LoadOne);
            req.ProcessRequest();
            return req.requestID;
        }

        public int LoadAllAssetAsync(string assetName, RequestCallBack callback = null)
        {
            AssetRequest req = CreateAssetRequest(assetName, callback, AssetRequestType.LoadAll);
            req.ProcessRequest();
            return req.requestID;
        }


        public int UnLoadAssetAsync(string assetName, RequestCallBack callback = null)
        {
            AssetRequest req = CreateAssetRequest(assetName, callback, AssetRequestType.UnloadOne);
            req.ProcessRequest();
            return req.requestID;
        }

        public void UnLoad(int reqID)
        {
            AssetRequest req = GetAssetRequest(reqID);
            if (req != null)
            {
                req.isCancel = true;
                if (!req.isRunning)
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
            _assetRequestMap.TryGetValue(reqID, out req);
            return req;
        }

        private AssetRequest CreateAssetRequest(string assetName, RequestCallBack callback, AssetRequestType type)
        {

            AssetInfo info = GetAssetInfo(assetName);


            AssetRequest req = null;
            if (_assetRequestCache.Count > 0)
                req = _assetRequestCache.Dequeue();
            else
                req = new AssetRequest(info, type, OnRequestFinish);

            req.requestID = ++_assetReqID;
            req.SetTaskFinishCallBack(callback);

            _assetRequestMap.Add(req.requestID, req);

            return req;
        }

        private void OnRequestFinish(AssetRequest req)
        {
            if (req.isCancel)
                FreeLoadRequest(req);
            else
                _finishRequestQueue.Enqueue(req.requestID);
        }

        private void FreeLoadRequest(AssetRequest req)
        {
            if (_assetRequestMap.ContainsKey(req.requestID))
            {
                _assetRequestMap.Remove(req.requestID);
            }

            req.Reset();

            _assetRequestCache.Enqueue(req);
        }
        #endregion


        public GameObject CreateAsset(string assetName)
        {
            GameObject viewObj = GlobalCenter.GetModule<AssetManager>().GetAssetObjWithType<GameObject>(assetName);
            GameObject go = GameObject.Instantiate<GameObject>(viewObj);

            return go;
        }


        public void DestoryAsset(GameObject go)
        {
            UnityEngine.GameObject.Destroy(go);
        }

    }

}