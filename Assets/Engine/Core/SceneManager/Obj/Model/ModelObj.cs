

using GameFramework.Asset;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Scene
{
    internal class ModelObj
    {
        private int _id;
        private int _modelType;

        private Action _loadCallBack;
        private Dictionary<string,AssetRequest> _reqAnimDict = new Dictionary<string,AssetRequest>();
        private AssetRequest _req;
        private GameObject _obj;


        public ModelObj(int modelType )
        {
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

            if (_obj != null)
            {
                AssetManager.UnLoadAssetAsync(_req);
                SceneManager.DestroyLayout(_obj);
            }

            string modelPath = GetModelPath(_modelType, id);
            string modelName = id.ToString();

            _loadCallBack = cb;

            _req = AssetManager.LoadAssetAsync(modelPath, modelName, OnLoadResFinish);

        }

        public void SetParent(Transform parent)
        {
            parent.AddChild(this._obj.transform);
        }


        public void PlayAnim(string clipName)
        {

            if (_reqAnimDict.TryGetValue(clipName,out AssetRequest _reqAnim))
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
                string clipPath = GetAnimPath(_modelType, clipName);
                _reqAnim = AssetManager.LoadAssetAsync(clipPath, clipName, OnLoadAnimFinish);
                _reqAnimDict[clipName] = _reqAnim;
            }
        }

        public bool IsLoade()
        {
            return _obj != null;
        }

        public static string GetModelPath(int modelType, int id)
        {
            return string.Format("Model/Role/{0}.ab", id);
        }

        public static string GetAnimPath(int modelType, string clipName)
        {
            return string.Format("Anim.ab", clipName);
        }
    }
}
