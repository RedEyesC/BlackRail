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
        private ModelType _modelType;
        private int _modelID;

        private Action<ModelObj> _loadCallBack;
        private Dictionary<string, AssetRequest> _reqAnimDict = new Dictionary<string, AssetRequest>();
        private Dictionary<string, Action> _pendingAnimEndCallbackDict = new Dictionary<string, Action>();
        private Dictionary<string, PendingLinearMixerAnim> _pendingLinearMixerAnimDict = new Dictionary<string, PendingLinearMixerAnim>();
        private HashSet<string> _pendingClipPlaySet = new HashSet<string>();
        private AssetRequest _req;
        private GameObject _obj;

        private Animator _animator;
        private PlayableGraph _graph;
        private AnimationPlayableOutput _animationOutput;

        public ModelType modelType => _modelType;
        public Animator animator => _animator;

        public ModelObj(BodyType bodyType, ModelType modelType)
        {
            _bodyType = bodyType;
            _modelType = modelType;
        }

        private sealed class PendingLinearMixerAnim
        {
            public string name;
            public string[] clipNames;
            public float[] thresholds;
            public float parameter;
            public bool extrapolateSpeed;
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
                Action onEnd = null;
                bool shouldPlayClip = _pendingClipPlaySet.Remove(assetRequest.assetName);
                if (_pendingAnimEndCallbackDict.TryGetValue(assetRequest.assetName, out onEnd))
                {
                    _pendingAnimEndCallbackDict.Remove(assetRequest.assetName);
                    shouldPlayClip = true;
                }

                if (shouldPlayClip)
                {
                    PlayLoadedAnim(clip, onEnd);
                }

                TryPlayPendingLinearMixerAnims();
            }
            else if (assetRequest != null)
            {
                Action onEnd = null;
                _pendingClipPlaySet.Remove(assetRequest.assetName);
                if (_pendingAnimEndCallbackDict.TryGetValue(assetRequest.assetName, out onEnd))
                {
                    _pendingAnimEndCallbackDict.Remove(assetRequest.assetName);
                    onEnd?.Invoke();
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

        public void PlayAnim(string clipName, Action onEnd = null)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                onEnd?.Invoke();
                return;
            }

            if (_reqAnimDict.TryGetValue(clipName, out AssetRequest _reqAnim))
            {
                if (_reqAnim.isDone)
                {
                    AnimationClip clip = AssetManager.GetAssetObjWithType<AnimationClip>(_reqAnim.bundleName, _reqAnim.assetName);
                    PlayLoadedAnim(clip, onEnd);
                }
                else if (onEnd != null)
                {
                    _pendingAnimEndCallbackDict[clipName] = onEnd;
                    _pendingClipPlaySet.Add(clipName);
                }
                else
                {
                    _pendingClipPlaySet.Add(clipName);
                }
            }
            else
            {
                string clipPath = GetAnimPath(_bodyType, _modelType, _modelID, clipName);
                _pendingClipPlaySet.Add(clipName);
                if (onEnd != null)
                {
                    _pendingAnimEndCallbackDict[clipName] = onEnd;
                }

                _reqAnim = AssetManager.LoadAssetAsync(clipPath, clipName, OnLoadAnimFinish);
                _reqAnimDict[clipName] = _reqAnim;
            }
        }

        private void PlayLoadedAnim(AnimationClip clip, Action onEnd)
        {
            if (_obj == null || clip == null)
            {
                onEnd?.Invoke();
                return;
            }

            AnimPlayableComponent animationPlayer = _obj.GetComponent<AnimPlayableComponent>();
            if (animationPlayer == null)
            {
                animationPlayer = _obj.AddComponent<AnimPlayableComponent>();
            }

            if (!animationPlayer.IsGraphInitialized)
            {
                animationPlayer.Initialize();
            }

            AnimPlayableComponent.State state = animationPlayer.Play(clip, 0f, true);
            if (state != null && onEnd != null)
            {
                state.EndNormalizedTime = 1f;
                state.OnEnd = onEnd;
            }
            else if (state == null)
            {
                onEnd?.Invoke();
            }
        }

        public void PlayLinearMixerAnim(
            string name,
            string[] clipNames,
            float[] thresholds,
            float parameter,
            bool extrapolateSpeed = false)
        {
            if (string.IsNullOrEmpty(name) ||
                clipNames == null ||
                thresholds == null ||
                clipNames.Length == 0 ||
                clipNames.Length != thresholds.Length)
            {
                return;
            }

            for (int i = 0; i < clipNames.Length; i++)
            {
                if (string.IsNullOrEmpty(clipNames[i]))
                {
                    return;
                }
            }

            RequestAnimClips(clipNames);
            if (!AreAnimClipsLoaded(clipNames))
            {
                _pendingLinearMixerAnimDict[name] = new PendingLinearMixerAnim
                {
                    name = name,
                    clipNames = CopyStringArray(clipNames),
                    thresholds = CopyFloatArray(thresholds),
                    parameter = parameter,
                    extrapolateSpeed = extrapolateSpeed
                };
                return;
            }

            PlayLoadedLinearMixerAnim(name, clipNames, thresholds, parameter, extrapolateSpeed);
        }

        private void RequestAnimClips(string[] clipNames)
        {
            for (int i = 0; i < clipNames.Length; i++)
            {
                string clipName = clipNames[i];
                if (_reqAnimDict.ContainsKey(clipName))
                {
                    continue;
                }

                string clipPath = GetAnimPath(_bodyType, _modelType, _modelID, clipName);
                _reqAnimDict[clipName] = AssetManager.LoadAssetAsync(clipPath, clipName, OnLoadAnimFinish);
            }
        }

        private bool AreAnimClipsLoaded(string[] clipNames)
        {
            for (int i = 0; i < clipNames.Length; i++)
            {
                if (!_reqAnimDict.TryGetValue(clipNames[i], out AssetRequest req) || !req.isDone)
                {
                    return false;
                }
            }

            return true;
        }

        private void PlayLoadedLinearMixerAnim(
            string name,
            string[] clipNames,
            float[] thresholds,
            float parameter,
            bool extrapolateSpeed)
        {
            if (_obj == null)
            {
                return;
            }

            AnimPlayableComponent animationPlayer = _obj.GetComponent<AnimPlayableComponent>();
            if (animationPlayer == null)
            {
                animationPlayer = _obj.AddComponent<AnimPlayableComponent>();
            }

            if (!animationPlayer.IsGraphInitialized)
            {
                animationPlayer.Initialize();
            }

            AnimPlayableComponent.LinearMixerChild[] children = new AnimPlayableComponent.LinearMixerChild[clipNames.Length];
            for (int i = 0; i < clipNames.Length; i++)
            {
                AssetRequest req = _reqAnimDict[clipNames[i]];
                AnimationClip clip = AssetManager.GetAssetObjWithType<AnimationClip>(req.bundleName, req.assetName);
                if (clip == null)
                {
                    return;
                }

                children[i] = new AnimPlayableComponent.LinearMixerChild(clip, thresholds[i]);
            }

            var transition = new AnimPlayableComponent.LinearMixerTransition(
                children,
                parameter,
                extrapolateSpeed,
                name)
            {
                Restart = false
            };
            animationPlayer.Play(transition);
        }

        private void TryPlayPendingLinearMixerAnims()
        {
            if (_pendingLinearMixerAnimDict.Count == 0)
            {
                return;
            }

            List<string> keys = new List<string>(_pendingLinearMixerAnimDict.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                PendingLinearMixerAnim pending = _pendingLinearMixerAnimDict[key];
                if (!AreAnimClipsLoaded(pending.clipNames))
                {
                    continue;
                }

                _pendingLinearMixerAnimDict.Remove(key);
                PlayLoadedLinearMixerAnim(
                    pending.name,
                    pending.clipNames,
                    pending.thresholds,
                    pending.parameter,
                    pending.extrapolateSpeed);
            }
        }

        private static string[] CopyStringArray(string[] source)
        {
            string[] result = new string[source.Length];
            Array.Copy(source, result, source.Length);
            return result;
        }

        private static float[] CopyFloatArray(float[] source)
        {
            float[] result = new float[source.Length];
            Array.Copy(source, result, source.Length);
            return result;
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
