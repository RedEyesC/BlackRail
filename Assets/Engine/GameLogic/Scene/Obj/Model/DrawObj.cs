using System;
using System.Collections.Generic;
using GameFramework.Interface;
using UnityEngine;

namespace GameLogic
{
    public enum BodyType
    {
        Role,
        Monster,
    }

    [Flags]
    public enum ModelType
    {
        Body = 1,
    }

    internal class DrawObj
    {
        private Dictionary<ModelType, ModelObj> _modelList = new Dictionary<ModelType, ModelObj>();
        private GameObject _rootObj;

        private BodyType _bodyType;

        private Action<ModelObj> _modelChangeCallback;

        public UnityEngine.Transform root
        {
            get { return _rootObj.transform; }
        }

        public DrawObj(BodyType bodyType)
        {
            Init(bodyType);
        }

        protected void Init(BodyType bodyType)
        {
            InitRootObj();

            _bodyType = bodyType;
        }

        protected void InitRootObj()
        {
            _rootObj = new GameObject();
            CameraCtrl.AddToObjRoot(_rootObj.transform);
        }

        public void SetModelID(ModelType modelType, int id)
        {
            if (!_modelList.ContainsKey(modelType))
            {
                _modelList.Add(modelType, new ModelObj(_bodyType, modelType));
            }

            ModelObj model = _modelList[modelType];

            model.ChangeModel(id, ChangeModelFunc);
        }

        public ModelObj GetModelByType(ModelType modelType)
        {
            return _modelList[modelType];
        }

        public void ChangeModelFunc(ModelObj model)
        {
            model.SetParent(_rootObj.transform);

            if (_modelChangeCallback != null)
            {
                _modelChangeCallback(model);
            }
        }

        public void SetModelChangeCallback(Action<ModelObj> callback)
        {
            _modelChangeCallback = callback;
        }

        public AnimationResourceStatus RequestAnimation(ModelType modelType, string name)
        {
            if (_rootObj == null)
            {
                return AnimationResourceStatus.Invalid;
            }

            bool hasTargetModel = false;
            AnimationResourceStatus result = AnimationResourceStatus.Ready;
            foreach (KeyValuePair<ModelType, ModelObj> kvp in _modelList)
            {
                if ((modelType & kvp.Key) == 0)
                {
                    continue;
                }

                hasTargetModel = true;
                AnimationResourceStatus status = kvp.Value.RequestAnimation(name);
                if (status == AnimationResourceStatus.Invalid || status == AnimationResourceStatus.Failed)
                {
                    result = status;
                }
                else if (status == AnimationResourceStatus.Loading && result == AnimationResourceStatus.Ready)
                {
                    result = AnimationResourceStatus.Loading;
                }
            }

            return hasTargetModel ? result : AnimationResourceStatus.Invalid;
        }

        public AnimationResourceStatus RequestAnimation(AnimPlayableComponent.LinearMixerTransition transition)
        {
            if (_rootObj == null || transition == null)
            {
                return AnimationResourceStatus.Invalid;
            }

            bool hasTargetModel = false;
            AnimationResourceStatus result = AnimationResourceStatus.Ready;
            foreach (KeyValuePair<ModelType, ModelObj> kvp in _modelList)
            {
                hasTargetModel = true;
                AnimationResourceStatus status = kvp.Value.RequestAnimation(transition);
                if (status == AnimationResourceStatus.Invalid || status == AnimationResourceStatus.Failed)
                {
                    result = status;
                }
                else if (status == AnimationResourceStatus.Loading && result == AnimationResourceStatus.Ready)
                {
                    result = AnimationResourceStatus.Loading;
                }
            }

            return hasTargetModel ? result : AnimationResourceStatus.Invalid;
        }

        public AnimationPlayResult TryPlayAnimation(
            ModelType modelType,
            string name,
            out AnimPlayableComponent.State firstState
        )
        {
            firstState = null;
            AnimationPlayResult result = AnimationPlayResult.Invalid;
            foreach (KeyValuePair<ModelType, ModelObj> kvp in _modelList)
            {
                if ((modelType & kvp.Key) == 0)
                {
                    continue;
                }

                AnimationPlayResult modelResult = kvp.Value.TryPlayAnimation(name, out AnimPlayableComponent.State state);
                if (modelResult == AnimationPlayResult.Played && firstState == null)
                {
                    firstState = state;
                }

                result = MergePlayResult(result, modelResult);
            }

            return result;
        }

        public AnimationPlayResult TryPlayAnimation(
            AnimPlayableComponent.LinearMixerTransition transition,
            out AnimPlayableComponent.State firstState
        )
        {
            firstState = null;
            if (_rootObj == null || transition == null)
            {
                return AnimationPlayResult.Invalid;
            }

            AnimationPlayResult result = AnimationPlayResult.Invalid;
            foreach (KeyValuePair<ModelType, ModelObj> kvp in _modelList)
            {
                AnimationPlayResult modelResult = kvp.Value.TryPlayAnimation(
                    transition,
                    out AnimPlayableComponent.State state
                );
                if (modelResult == AnimationPlayResult.Played && firstState == null)
                {
                    firstState = state;
                }

                result = MergePlayResult(result, modelResult);
            }

            return result;
        }

        private static AnimationPlayResult MergePlayResult(AnimationPlayResult current, AnimationPlayResult next)
        {
            if (current == AnimationPlayResult.Played || next == AnimationPlayResult.Played)
            {
                return AnimationPlayResult.Played;
            }

            if (current == AnimationPlayResult.NotReady || next == AnimationPlayResult.NotReady)
            {
                return AnimationPlayResult.NotReady;
            }

            if (current == AnimationPlayResult.NotRequested || next == AnimationPlayResult.NotRequested)
            {
                return AnimationPlayResult.NotRequested;
            }

            if (current == AnimationPlayResult.Failed || next == AnimationPlayResult.Failed)
            {
                return AnimationPlayResult.Failed;
            }

            return AnimationPlayResult.Invalid;
        }

        public void Update(float nowTime, float elapseSeconds)
        {
            if (_rootObj == null || elapseSeconds <= 0f)
            {
                return;
            }

            foreach (KeyValuePair<ModelType, ModelObj> kvp in _modelList)
            {
                kvp.Value.Update(nowTime, elapseSeconds);
            }
        }

        public bool IsLoade()
        {
            if (_rootObj != null)
            {
                foreach (ModelObj model in _modelList.Values)
                {
                    if (!model.IsLoade())
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public void Rest()
        {
            FreeModel();
            FreeRootObj();
        }

        private void FreeModel()
        {
            //TODO
        }

        private void FreeRootObj()
        {
            if (_rootObj != null)
            {
                CameraCtrl.DestroyLayout(_rootObj);
                _rootObj = null;
            }
        }
    }
}
