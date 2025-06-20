

using GameFramework.Asset;
using GameFramework.Scene;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameLogic
{
    internal class ModelObj
    {
        private BodyType _bodyType;
        private int _modelType;

        private int _id;


        private Action _loadCallBack;
        private Dictionary<string, AssetRequest> _reqAnimDict = new Dictionary<string, AssetRequest>();
        private AssetRequest _req;
        private GameObject _obj;


        public ModelObj(BodyType bodyType, int modelType)
        {
            _bodyType = bodyType;
            _modelType = modelType;
        }


        private void OnLoadResFinish(Request req)
        {
            AssetRequest assetRequest = req as AssetRequest;
            if (req.isDone)
            {
                GameObject Obj = AssetManager.GetAssetObjWithType<GameObject>(assetRequest.bundleName, assetRequest.assetName);
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
                    _obj.GetComponent<AnimPlayableComponent>().Play(clip, clip.name);
                }

            }
        }

        public void ChangeModel(int id, System.Action cb = null)
        {


            if (_id == id)
            {
                return;
            }

            _id = id;

            if (_obj != null)
            {
                AssetManager.UnLoadAssetAsync(_req);
                SceneManager.DestroyLayout(_obj);
            }

            string modelPath = GetModelPath(_bodyType, _modelType, id);
            string modelName = GetModelName(id);

            _loadCallBack = cb;

            _req = AssetManager.LoadAssetAsync(modelPath, modelName, OnLoadResFinish);

        }

        public void SetParent(Transform parent)
        {
            parent.AddChild(this._obj.transform);
        }


        public void PlayAnim(string clipName)
        {

            if (_reqAnimDict.TryGetValue(clipName, out AssetRequest _reqAnim))
            {
                if (_reqAnim.isDone)
                {
                    if (_obj != null)
                    {
                        _obj.GetComponent<AnimPlayableComponent>().Play(clipName);
                    }
                }
            }
            else
            {
                string clipPath = GetAnimPath(_bodyType, _modelType, _id, clipName);
                _reqAnim = AssetManager.LoadAssetAsync(clipPath, clipName, OnLoadAnimFinish);
                _reqAnimDict[clipName] = _reqAnim;
            }
        }

        public bool IsLoade()
        {
            return _obj != null;
        }

        public static string GetModelPath(BodyType bodyType, int modelType, int id)
        {
            string path = "";
            switch (bodyType)
            {
                case BodyType.Role:
                    path = string.Format("Model/Role/{0}", id);
                    break;
                case BodyType.Monster:
                    path = string.Format("Model/Monster/{0}", id);
                    break;
            }

            return path;
        }
        public static string GetModelName(int id)
        {
            return string.Format("{0}", id);
        }

        public static string GetAnimPath(BodyType bodyType, int modelType,int id, string clipName)
        {
            string path = "";
            switch (bodyType)
            {
                case BodyType.Role:
                    path = string.Format("Anim", clipName); ;
                    break;
                case BodyType.Monster:
                    path = string.Format("Model/Monster/{0}", id);
                    break;
            }

            return path;
        }
    }
}
