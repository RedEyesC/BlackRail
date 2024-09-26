

using GameFramework.Asset;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Scene
{
    internal class ModelObj
    {
        private string _path;
        private string _name;
        private Action _loadCallBack;
        private List<AssetRequest> _reqAnimList = new List<AssetRequest>();
        private AssetRequest _req;
        private GameObject _obj;


        private void OnLoadResFinish(Request req)
        {
            if (req.isDone)
            {
                GameObject Obj = AssetManager.GetAssetObjWithType<GameObject>(_path, _name);
                _obj = GameObject.Instantiate<GameObject>(Obj);
            }

            if (_loadCallBack != null)
            {
                _loadCallBack();
            }

        }

        private void OnLoadAnimFinish(Request req)
        {
            AssetRequest assetRequest = req as AssetRequest;
            if (req.isDone)
            {
                AnimationClip clip = AssetManager.GetAssetObjWithType<AnimationClip>(assetRequest.bundleName, assetRequest.assetName);

                if (_obj != null)
                {
                    _obj.GetComponent<AnimPlayableComponent>().AddClip(clip, clip.name);
                }

            }
        }

        public void ChangeModel(string modelPath, string modelName, System.Action cb = null)
        {
            if (_path == modelPath && _name == modelName)
            {
                return;
            }

            if (_obj != null)
            {
                AssetManager.UnLoadAssetAsync(_req);
                SceneManager.DestroyLayout(_obj);
            }

            _path = modelPath;
            _name = modelName;

            _loadCallBack = cb;

            _req = AssetManager.LoadAssetAsync(_path, _name, OnLoadResFinish);

        }

        public void SetParent(Transform parent)
        {
            parent.AddChild(this._obj.transform);
        }


        public void PlayAnim(string name)
        {
            if (_obj != null)
            {
                _obj.GetComponent<AnimPlayableComponent>().Play(name);
            }
        }

        public void AddClip(string clipPath, string clipName)
        {
            AssetRequest _reqAnim = AssetManager.LoadAssetAsync(clipPath, clipName, OnLoadAnimFinish);
            _reqAnimList.Add(_reqAnim);
        }

        public bool IsLoade()
        {
            if (_req != null)
            {
                return _req.isDone;
            }

            return false;
        }
    }
}
