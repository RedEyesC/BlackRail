using System;
using System.Collections.Generic;
using GameFramework.Asset;
using GameFramework.Interface;
using GameFramework.Scene;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Animations;

namespace GameLogic
{
    internal class ModelObj
    {
        private BodyType _bodyType;
        private ModelType _modelType;
        private int _modelID;

        private Action<ModelObj> _loadCallBack;
        private Dictionary<string, AssetRequest> _reqAnimDict = new Dictionary<string, AssetRequest>();
        private Dictionary<object, PendingLinearMixerAnim> _pendingLinearMixerAnimDict = new Dictionary<object, PendingLinearMixerAnim>();
        private AssetRequest _req;
        private GameObject _obj;

        private Animator _animator;
        private AnimPlayableComponent _animationPlayer;

        public ModelType modelType => _modelType;
        public Animator animator => _animator;

        public ModelObj(BodyType bodyType, ModelType modelType)
        {
            _bodyType = bodyType;
            _modelType = modelType;
        }

        private sealed class PendingLinearMixerAnim
        {
            public AnimPlayableComponent.LinearMixerTransition transition;
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
                TryPlayPendingLinearMixerAnims();
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

        public AnimPlayableComponent.State PlayAnim(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                return null;
            }

            if (_reqAnimDict.TryGetValue(clipName, out AssetRequest _reqAnim))
            {
                if (_reqAnim.isDone)
                {
                    AnimationClip clip = AssetManager.GetAssetObjWithType<AnimationClip>(_reqAnim.bundleName, _reqAnim.assetName);
                    return PlayLoadedAnim(clip);
                }

                return null;
            }

            string clipPath = GetAnimPath(_bodyType, _modelType, _modelID, clipName);
            _reqAnim = AssetManager.LoadAssetAsync(clipPath, clipName, OnLoadAnimFinish);
            _reqAnimDict[clipName] = _reqAnim;
            return null;
        }

        private AnimPlayableComponent.State PlayLoadedAnim(AnimationClip clip)
        {
            if (_obj == null || clip == null)
            {
                return null;
            }

            if (_animationPlayer == null)
            {
                _animationPlayer = _obj.AddComponent<AnimPlayableComponent>();
            }

            if (!_animationPlayer.IsGraphInitialized)
            {
                _animationPlayer.Initialize();
            }

            return _animationPlayer.Play(clip, 0f, true);
        }

        public AnimPlayableComponent.State PlayAnim(AnimPlayableComponent.LinearMixerTransition transition)
        {
            if (transition == null || !CanPlayLinearMixerTransition(transition))
            {
                return null;
            }

            RequestAnimClips(transition.Children);
            if (!AreAnimClipsLoaded(transition.Children))
            {
                _pendingLinearMixerAnimDict[transition.Key] = new PendingLinearMixerAnim
                {
                    transition = CopyLinearMixerTransition(transition),
                };
                return null;
            }

            return PlayLoadedLinearMixerAnim(transition);
        }

        private bool CanPlayLinearMixerTransition(AnimPlayableComponent.LinearMixerTransition transition)
        {
            AnimPlayableComponent.LinearMixerChild[] children = transition.Children;
            if (children == null || children.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].clip == null && string.IsNullOrEmpty(children[i].name))
                {
                    return false;
                }
            }

            return true;
        }

        private void RequestAnimClips(AnimPlayableComponent.LinearMixerChild[] children)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].clip != null)
                {
                    continue;
                }

                string clipName = children[i].name;
                if (_reqAnimDict.ContainsKey(clipName))
                {
                    continue;
                }

                string clipPath = GetAnimPath(_bodyType, _modelType, _modelID, clipName);
                _reqAnimDict[clipName] = AssetManager.LoadAssetAsync(clipPath, clipName, OnLoadAnimFinish);
            }
        }

        private bool AreAnimClipsLoaded(AnimPlayableComponent.LinearMixerChild[] children)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].clip != null)
                {
                    continue;
                }

                if (!_reqAnimDict.TryGetValue(children[i].name, out AssetRequest req) || !req.isDone)
                {
                    return false;
                }
            }

            return true;
        }

        private AnimPlayableComponent.State PlayLoadedLinearMixerAnim(AnimPlayableComponent.LinearMixerTransition sourceTransition)
        {
            if (_obj == null)
            {
                return null;
            }

            if (_animationPlayer == null)
            {
                _animationPlayer = _obj.AddComponent<AnimPlayableComponent>();
            }

            if (!_animationPlayer.IsGraphInitialized)
            {
                _animationPlayer.Initialize();
            }

            AnimPlayableComponent.LinearMixerChild[] sourceChildren = sourceTransition.Children;
            for (int i = 0; i < sourceChildren.Length; i++)
            {
                AnimPlayableComponent.LinearMixerChild sourceChild = sourceChildren[i];
                AnimationClip clip = sourceChild.clip;
                if (clip == null)
                {
                    AssetRequest req = _reqAnimDict[sourceChild.name];
                    clip = AssetManager.GetAssetObjWithType<AnimationClip>(req.bundleName, req.assetName);
                }

                if (clip == null)
                {
                    return null;
                }

                sourceChild.clip = clip;

                sourceChildren[i] = sourceChild;
            }

            return _animationPlayer.Play(sourceTransition);
        }

        private void TryPlayPendingLinearMixerAnims()
        {
            if (_pendingLinearMixerAnimDict.Count == 0)
            {
                return;
            }

            List<object> keys = new List<object>(_pendingLinearMixerAnimDict.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                object key = keys[i];
                PendingLinearMixerAnim pending = _pendingLinearMixerAnimDict[key];
                if (pending.transition == null || !AreAnimClipsLoaded(pending.transition.Children))
                {
                    continue;
                }

                _pendingLinearMixerAnimDict.Remove(key);
                PlayLoadedLinearMixerAnim(pending.transition);
            }
        }

        public void Update(float nowTime, float elapseSeconds)
        {
            if (_obj == null || elapseSeconds <= 0f)
            {
                return;
            }

            if (_animationPlayer)
            {
                _animationPlayer.UpdateGraph(elapseSeconds);
            }
        }

        private static AnimPlayableComponent.LinearMixerChild[] CopyLinearMixerChildren(AnimPlayableComponent.LinearMixerChild[] source)
        {
            AnimPlayableComponent.LinearMixerChild[] result = new AnimPlayableComponent.LinearMixerChild[source.Length];
            Array.Copy(source, result, source.Length);
            return result;
        }

        private static AnimPlayableComponent.LinearMixerTransition CopyLinearMixerTransition(
            AnimPlayableComponent.LinearMixerTransition source
        )
        {
            return new AnimPlayableComponent.LinearMixerTransition(
                CopyLinearMixerChildren(source.Children),
                source.DefaultParameter,
                source.ExtrapolateSpeed,
                source.Key
            )
            {
                FadeDuration = source.FadeDuration,
                Restart = source.Restart,
            };
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

        public static string GetModelPath(BodyType bodyType, ModelType modelType, int id)
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

        public static string GetAnimPath(BodyType bodyType, ModelType modelType, int id, string clipName)
        {
            string path = "";
            switch (bodyType)
            {
                case BodyType.Role:
                    path = string.Format("Anim/0", clipName);
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
