using UnityEngine;
using System.Collections.Generic;
using System;

namespace GameFramework.Runtime
{
    public class AssetInfo
    {
        public enum AssetState
        {
            Unload,
            Loading,
            Loaded,
        }

        public enum DownloadState
        {
            None,
            Downloading,
            Downloaded,
        }

        private string _AssetName;
        private Type _AssetType;
        private int _Priority;


        //private static int sAssetBundleNum = 0;
        //public static int AssetBundleNum { get { return sAssetBundleNum; } }

        public string AssetName { get { return _AssetName; } }
        public AssetState State { get; private set; }

        //private int mRefCount = 0;
        //private bool mResourceMode = false;

        private UnityEngine.Object _AssetObj = null;
        private ResourceRequest _AssetRequest = null;


        public AssetInfo(string assetName, Type assetType, int priority)
        {
            _AssetName = assetName;
            _AssetType = assetType;
            _Priority = priority;
        }


        #region Get Asset
        public bool IsAssetObjLoaded()
        {
            if (_AssetObj != null)
                return _AssetObj;
            return false;
        }
        #endregion



        #region Asset Operate

        public AsyncOperation LoadAssetAsync(string assetName)
        {
            if (_AssetRequest == null)
            {
                _AssetRequest = Resources.LoadAsync(assetName);
            }

            return _AssetRequest;
        }

        public void OnAssetObjLoaded()
        {
            if (_AssetObj == null)
            {
                if (_AssetRequest != null)
                {
                    _AssetObj = _AssetRequest.asset;
                }

            }
        }

        public bool TryUnloadAsset()
        {

            switch (State)
            {
                case AssetState.Unload:
                    return true;
                case AssetState.Loading:
                    return false;
                case AssetState.Loaded:
                    UnloadSelf();
                    return true;
            }

            return false;
        }

        public void UnloadSelf()
        {
            if (State != AssetState.Loaded)
            {
                Debug.LogErrorFormat("Invalid Unload Bundle {0}", _AssetName);
            }


            if (_AssetObj != null)
            {
                Resources.UnloadAsset(_AssetObj);
            }

            State = AssetState.Unload;
            _AssetObj = null;
            _AssetRequest = null;
        }


        public bool IsLoaded
        {
            get
            {
                return State == AssetState.Loaded;
            }
        }


        #endregion

    }

}