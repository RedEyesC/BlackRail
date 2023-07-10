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

        public string AssetName;
        
        public AssetState State = AssetState.Unload;


        private UnityEngine.Object _AssetObj = null;
        private ResourceRequest _AssetRequest = null;


        public AssetInfo(string assetName)
        {
            AssetName = assetName;
            State = AssetState.Unload;

        }


        #region Get Asset
        public bool IsAssetObjLoaded()
        {
            if (_AssetObj != null)
                return _AssetObj;
            return false;
        }

        public UnityEngine.Object GetAssetObj()
        {
            if (_AssetObj != null)
            {
                return _AssetObj;
            }
            return null;
        }

        public T GetAssetObjWithType<T>() where T : class
        {
            if (_AssetObj != null)
            {
                return _AssetObj as T;
            }
            return null;
        }

        #endregion


        #region Asset Operate

        public AsyncOperation LoadAssetAsync(string assetName)
        {
            if (_AssetRequest == null || State == AssetState.Unload)
            {
                State = AssetState.Loading;
                _AssetRequest = Resources.LoadAsync(assetName);
            }

            return _AssetRequest;
        }

        public void OnAssetObjLoaded()
        {

            State = AssetState.Loaded;

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
                Debug.LogErrorFormat("Invalid Unload Bundle {0}", AssetName);
            }


            if (_AssetObj != null)
            {
                Resources.UnloadAsset(_AssetObj);
            }

             Reset();
        }


        public void Reset()
        {
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