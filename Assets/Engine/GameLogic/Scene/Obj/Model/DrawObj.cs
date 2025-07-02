using GameFramework.Scene;
using System.Collections.Generic;
using UnityEngine;


namespace GameLogic
{
    internal class DrawObj
    {

        private Dictionary<int, ModelObj> _modelList = new Dictionary<int, ModelObj>();
        private GameObject _rootObj;

        private BodyType _bodyType;

        public UnityEngine.Transform root
        {
            get
            {
                return _rootObj.transform;
            }
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


        public void SetModelID(int modelType, int id)
        {

            if (!_modelList.ContainsKey(modelType))
            {
                _modelList.Add(modelType, new ModelObj(_bodyType,modelType));
            }

            ModelObj model = _modelList[modelType];

            model.ChangeModel(id, () =>
            {
                model.SetParent(_rootObj.transform);
            });

        }


        public void PlayAnim(string name)
        {

            if (_rootObj != null)
            {
                foreach (KeyValuePair<int, ModelObj> kvp in _modelList)
                {
                    kvp.Value.PlayAnim(name);
                }
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