using System;
using GameFramework.Interface;
using UnityEngine;

namespace GameLogic
{
    internal class Obj
    {
        protected DrawObj _drawObj;

        protected Vector2 _dir = new Vector2();
        protected Vector3 _pos = new Vector3();

        public float speed = 0f;

        public UnityEngine.Transform root
        {
            get { return _drawObj.root; }
        }

        public Obj(BodyType bodyType)
        {
            Init(bodyType);
        }

        public virtual void Init(BodyType bodyType)
        {
            _drawObj = new DrawObj(bodyType);
        }

        public virtual void Rest() { }

        public virtual void EarlyUpdate() { }

        public virtual void Update(float nowTime, float elapseSeconds)
        {
            _drawObj.Update(nowTime, elapseSeconds);
        }

        public void SetModelID(ModelType modelType, int id)
        {
            _drawObj.SetModelID(modelType, id);
        }

        public void GetModelByType(ModelType modelType)
        {
            _drawObj.GetModelByType(modelType);
        }

        public void SetModelChangeCallback(Action<ModelObj> callback)
        {
            _drawObj.SetModelChangeCallback(callback);
        }

        public AnimationResourceStatus RequestAnimation(string name)
        {
            return RequestAnimation(ModelType.Body, name);
        }

        public AnimationResourceStatus RequestAnimation(ModelType modelType, string name)
        {
            return _drawObj.RequestAnimation(modelType, name);
        }

        public AnimationResourceStatus RequestAnimation(AnimPlayableComponent.LinearMixerTransition transition)
        {
            return _drawObj.RequestAnimation(transition);
        }

        public AnimationPlayResult TryPlayAnimation(string name, out AnimPlayableComponent.State state)
        {
            return TryPlayAnimation(ModelType.Body, name, out state);
        }

        public AnimationPlayResult TryPlayAnimation(
            ModelType modelType,
            string name,
            out AnimPlayableComponent.State state
        )
        {
            return _drawObj.TryPlayAnimation(modelType, name, out state);
        }

        public AnimationPlayResult TryPlayAnimation(
            AnimPlayableComponent.LinearMixerTransition transition,
            out AnimPlayableComponent.State state
        )
        {
            return _drawObj.TryPlayAnimation(transition, out state);
        }

        public bool IsLoade()
        {
            return _drawObj.IsLoade();
        }

        public virtual void Destroy()
        {
            if (_drawObj != null)
            {
                _drawObj.Rest();
                _drawObj = null;
            }
        }

        public void SetPosition(float x, float y, float z)
        {
            _pos.Set(x, y, z);
            _drawObj.root.position = _pos;
        }

        public void SetDir(float x, float y)
        {
            if (x != 0 || y != 0)
            {
                _dir.Set(x, y);
                _dir.Normalize();
                _drawObj.root.SetLookDir(_dir.x, 0, _dir.y);
            }
        }
    }
}
