using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

namespace GameFramework.Runtime
{
    public class AssetInfo
    {
        public enum Assetstate
        {
            Unload,
            Loading,
            Loaded,
        }

        public string assetName;
        
        public Assetstate state = Assetstate.Unload;


        private UnityEngine.Object _assetObj = null;
        private AsyncOperation _assetRequest = null;


        public AssetInfo(string assetName)
        {
            this.assetName = assetName;
            this.state = Assetstate.Unload;

        }


        #region Get Asset
        public bool IsAssetObjLoaded()
        {
            if (_assetObj != null)
                return _assetObj;
            return false;
        }

        public UnityEngine.Object GetAssetObj()
        {
            if (_assetObj != null)
            {
                return _assetObj;
            }
            return null;
        }

        public T GetAssetObjWithType<T>() where T : class
        {
            if (_assetObj != null)
            {
                return _assetObj as T;
            }
            return null;
        }

        #endregion


        #region Asset Operate

        public AsyncOperation LoadAssetAsync(string assetName)
        {
            if (_assetRequest == null || state == Assetstate.Unload)
            {
                state = Assetstate.Loading;
                _assetRequest = Resources.LoadAsync(assetName);
            }

            return _assetRequest;
        }

        public void OnAssetObjLoaded()
        {

            state = Assetstate.Loaded;

            if (_assetObj == null)
            {
                if (_assetRequest != null)
                {
                    ResourceRequest req = (ResourceRequest) _assetRequest;
                    _assetObj = req.asset;
                }

            }
        }

        public bool UnloadAsset()
        {

            switch (state)
            {
                case Assetstate.Unload:
                    return true;
                case Assetstate.Loading:
                    return false;
                case Assetstate.Loaded:

                    if (_assetObj != null)
                    {
                        Resources.UnloadAsset(_assetObj);
                    } 
                    return true;
            }

            return false;
        }

        public void Reset()
        {
            state = Assetstate.Unload;
            _assetObj = null;
            _assetRequest = null;
        }

        public bool IsLoaded
        {
            get
            {
                return state == Assetstate.Loaded;
            }
        }

        #endregion


        #region  Load Scene
        public AsyncOperation LoadSceneAsync(string assetName)
        {
            if (_assetRequest == null || state == Assetstate.Unload)
            {
                state = Assetstate.Loading;
                _assetRequest = SceneManager.LoadSceneAsync(assetName, LoadSceneMode.Additive);
            }

            return _assetRequest;
        }

        public void OnSceneLoaded()
        {

            state = Assetstate.Loaded;

        }

        public AsyncOperation UnloadScene()
        {
            _assetRequest = SceneManager.UnloadSceneAsync(assetName);

            return _assetRequest;
        }

        #endregion
    }

}