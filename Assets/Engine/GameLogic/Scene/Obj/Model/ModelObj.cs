using System;
using System.Collections.Generic;
using GameFramework.Asset;
using GameFramework.Interface;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Animations;

namespace GameLogic
{
    internal enum AnimationResourceStatus
    {
        Invalid,
        NotRequested,
        Loading,
        Ready,
        Failed,
    }

    internal enum AnimationPlayResult
    {
        Invalid,
        NotRequested,
        NotReady,
        Failed,
        Played,
    }

    internal class ModelObj
    {
        private BodyType _bodyType;
        private ModelType _modelType;
        private int _modelID;

        private Action<ModelObj> _loadCallBack;
        private readonly Dictionary<string, AssetRequest> _reqAnimDict = new Dictionary<string, AssetRequest>();
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

        public void ChangeModel(int id, Action<ModelObj> cb = null)
        {
            if (_modelID == id)
            {
                return;
            }

            _modelID = id;

            if (_obj != null)
            {
                ReleaseAnimationRequests();
                AssetManager.UnLoadAssetAsync(_req);
                CameraCtrl.DestroyLayout(_obj);
                _obj = null;
                _animator = null;
                _animationPlayer = null;
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

        public AnimationResourceStatus RequestAnimation(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                return AnimationResourceStatus.Invalid;
            }

            if (_reqAnimDict.TryGetValue(clipName, out AssetRequest request))
            {
                return GetAnimationResourceStatus(request);
            }

            string clipPath = GetAnimPath(_bodyType, _modelType, _modelID, clipName);
            request = AssetManager.LoadAssetAsync(clipPath, clipName);
            _reqAnimDict[clipName] = request;
            return GetAnimationResourceStatus(request);
        }

        public AnimationResourceStatus RequestAnimation(AnimPlayableComponent.LinearMixerTransition transition)
        {
            if (!CanPlayLinearMixerTransition(transition))
            {
                return AnimationResourceStatus.Invalid;
            }

            AnimationResourceStatus result = AnimationResourceStatus.Ready;
            AnimPlayableComponent.LinearMixerChild[] children = transition.Children;
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].clip != null)
                {
                    continue;
                }

                AnimationResourceStatus childStatus = RequestAnimation(children[i].name);
                if (childStatus == AnimationResourceStatus.Invalid || childStatus == AnimationResourceStatus.Failed)
                {
                    result = childStatus;
                }
                else if (childStatus == AnimationResourceStatus.Loading && result == AnimationResourceStatus.Ready)
                {
                    result = AnimationResourceStatus.Loading;
                }
            }

            return result;
        }

        public AnimationPlayResult TryPlayAnimation(string clipName, out AnimPlayableComponent.State state)
        {
            state = null;
            AnimationResourceStatus status = TryGetLoadedAnimationClip(clipName, out AnimationClip clip);
            if (status != AnimationResourceStatus.Ready)
            {
                return GetAnimationPlayResult(status);
            }

            state = PlayLoadedAnim(clip);
            return state != null ? AnimationPlayResult.Played : AnimationPlayResult.NotReady;
        }

        public AnimationPlayResult TryPlayAnimation(
            AnimPlayableComponent.LinearMixerTransition transition,
            out AnimPlayableComponent.State state
        )
        {
            state = null;
            if (!CanPlayLinearMixerTransition(transition))
            {
                return AnimationPlayResult.Invalid;
            }

            AnimPlayableComponent.LinearMixerTransition resolvedTransition = CopyLinearMixerTransition(transition);
            AnimPlayableComponent.LinearMixerChild[] children = resolvedTransition.Children;
            for (int i = 0; i < children.Length; i++)
            {
                AnimPlayableComponent.LinearMixerChild child = children[i];
                if (child.clip != null)
                {
                    continue;
                }

                AnimationResourceStatus childStatus = TryGetLoadedAnimationClip(child.name, out AnimationClip clip);
                if (childStatus != AnimationResourceStatus.Ready)
                {
                    return GetAnimationPlayResult(childStatus);
                }

                child.clip = clip;
                children[i] = child;
            }

            AnimPlayableComponent animationPlayer = GetAnimationPlayer();
            if (animationPlayer == null)
            {
                return AnimationPlayResult.NotReady;
            }

            state = animationPlayer.Play(resolvedTransition);
            return state != null ? AnimationPlayResult.Played : AnimationPlayResult.Failed;
        }

        private bool CanPlayLinearMixerTransition(AnimPlayableComponent.LinearMixerTransition transition)
        {
            if (transition == null)
            {
                return false;
            }

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

        private AnimationResourceStatus TryGetLoadedAnimationClip(string clipName, out AnimationClip clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(clipName))
            {
                return AnimationResourceStatus.Invalid;
            }

            if (!_reqAnimDict.TryGetValue(clipName, out AssetRequest request))
            {
                return AnimationResourceStatus.NotRequested;
            }

            AnimationResourceStatus status = GetAnimationResourceStatus(request);
            if (status != AnimationResourceStatus.Ready)
            {
                return status;
            }

            clip = GetLoadedAnimationClip(request);
            return clip != null ? AnimationResourceStatus.Ready : AnimationResourceStatus.Failed;
        }

        private static AnimationPlayResult GetAnimationPlayResult(AnimationResourceStatus status)
        {
            switch (status)
            {
                case AnimationResourceStatus.NotRequested:
                    return AnimationPlayResult.NotRequested;
                case AnimationResourceStatus.Loading:
                    return AnimationPlayResult.NotReady;
                case AnimationResourceStatus.Failed:
                    return AnimationPlayResult.Failed;
                default:
                    return AnimationPlayResult.Invalid;
            }
        }

        private static AnimationResourceStatus GetAnimationResourceStatus(AssetRequest request)
        {
            if (request == null)
            {
                return AnimationResourceStatus.Invalid;
            }

            if (!request.isDone)
            {
                return AnimationResourceStatus.Loading;
            }

            return request.result == Request.Result.Success
                ? AnimationResourceStatus.Ready
                : AnimationResourceStatus.Failed;
        }

        private static AnimationClip GetLoadedAnimationClip(AssetRequest request)
        {
            return request?.asset as AnimationClip;
        }

        private AnimPlayableComponent.State PlayLoadedAnim(AnimationClip clip)
        {
            AnimPlayableComponent animationPlayer = GetAnimationPlayer();
            return animationPlayer != null && clip != null ? animationPlayer.Play(clip, 0f, true) : null;
        }

        private AnimPlayableComponent GetAnimationPlayer()
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

            return _animationPlayer;
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

        private void ReleaseAnimationRequests()
        {
            foreach (AssetRequest request in _reqAnimDict.Values)
            {
                if (request != null)
                {
                    AssetManager.UnLoadAssetAsync(request);
                }
            }

            _reqAnimDict.Clear();
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
