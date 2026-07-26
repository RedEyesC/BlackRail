using System;
using System.Collections.Generic;
using GameFramework.Scene;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public enum BodyType
    {
        Role,
        Monster
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
            SceneManager.AddToObjRoot(_rootObj.transform);
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

        public void PlayLayerAnim(ModelType modelType, string name, Action onEnd = null)
        {
            if (_rootObj == null)
            {
                onEnd?.Invoke();
                return;
            }

            bool callbackInvoked = false;
            Action invokeOnce = onEnd == null
                ? null
                : () =>
                {
                    if (callbackInvoked)
                    {
                        return;
                    }

                    callbackInvoked = true;
                    onEnd();
                };

            bool hasModel = false;
            foreach (KeyValuePair<ModelType, ModelObj> kvp in _modelList)
            {
                if ((modelType & kvp.Key) == 0)
                {
                    continue;
                }

                hasModel = true;
                kvp.Value.PlayAnim(name, invokeOnce);
            }

            if (!hasModel)
            {
                invokeOnce?.Invoke();
            }
        }

        public void PlayLinearMixerAnim(
            string name,
            string[] clipNames,
            float[] thresholds,
            float parameter,
            bool extrapolateSpeed = false)
        {
            if (_rootObj == null)
            {
                return;
            }

            foreach (KeyValuePair<ModelType, ModelObj> kvp in _modelList)
            {
                kvp.Value.PlayLinearMixerAnim(name, clipNames, thresholds, parameter, extrapolateSpeed);
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
                SceneManager.DestroyLayout(_rootObj);
                _rootObj = null;
            }
        }
    }
}
