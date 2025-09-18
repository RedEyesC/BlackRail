using System;
using System.Collections.Generic;
using GameFramework.Asset;
using GameFramework.Scene;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GameLogic
{
    internal class ModelObj
    {
        private BodyType _bodyType;
        private int _modelType;
        private int _modelID;

        private Action<ModelObj> _loadCallBack;
        private Dictionary<string, AssetRequest> _reqAnimDict = new Dictionary<string, AssetRequest>();
        private AssetRequest _req;
        private GameObject _obj;

        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _animationOutput;

        public int modelType => _modelType;
        public Animator animator => _animator;

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
                _loadCallBack(this);
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

        public void ChangeModel(int id, Action<ModelObj> cb = null)
        {
            if (_modelID == id)
            {
                return;
            }

            _modelID = id;

            if (_obj != null)
            {
                AssetManager.UnLoadAssetAsync(_req);
                SceneManager.DestroyLayout(_obj);
            }

            string modelPath = GetModelPath(_bodyType, _modelType, _modelID);
            string modelName = GetModelName(_modelID);

            _loadCallBack = cb;

            _req = AssetManager.LoadAssetAsync(modelPath, modelName, OnLoadResFinish);
        }

        public void SetParent(Transform parent)
        {
            parent.AddChild(this._obj.transform);
        }

        public Transform[] GetComponentsInChildrenTransform()
        {
            return _obj.GetComponentsInChildren<Transform>();
        }

        public void CreatePlayableGraph<T>(T job)
            where T : struct, IAnimationJob
        {
            if (_graph.IsValid())
            {
                return;
            }

            _animator = _obj.GetComponent<Animator>();

            _graph = PlayableGraph.Create("PlayableGraph");
            _animationOutput = AnimationPlayableOutput.Create(_graph, "AnimationOutput", _animator);

            var playable = AnimationScriptPlayable.Create(_graph, job);
            _animationOutput.SetSourcePlayable(playable);
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
                string clipPath = GetAnimPath(_bodyType, _modelType, _modelID, clipName);
                _reqAnim = AssetManager.LoadAssetAsync(clipPath, clipName, OnLoadAnimFinish);
                _reqAnimDict[clipName] = _reqAnim;
            }
        }

        public void AddJobDependency(JobHandle jobHandle)
        {
            if (_obj != null)
            {
                _obj.GetComponent<Animator>().AddJobDependency(jobHandle);
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

        public static string GetAnimPath(BodyType bodyType, int modelType, int id, string clipName)
        {
            string path = "";
            switch (bodyType)
            {
                case BodyType.Role:
                    path = string.Format("Anim", clipName);
                    ;
                    break;
                case BodyType.Monster:
                    path = string.Format("Model/Monster/{0}", id);
                    break;
            }

            return path;
        }
    }
}
