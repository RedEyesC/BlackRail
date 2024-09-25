using System.Collections.Generic;
using UnityEngine;


namespace GameFramework.Scene
{
    internal class DrawObj
    {

        private Dictionary<int, ModelObj> _modelList = new Dictionary<int, ModelObj>();
        private GameObject _rootObj;


        public UnityEngine.Transform root
        {
            get
            {
                return _rootObj.transform;
            }
        }

        public DrawObj()
        {
            Init();
        }

        protected void Init()
        {
            InitRootObj();
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
                _modelList.Add(modelType, new ModelObj());
            }


            ModelObj model = _modelList[modelType];

            string path = GetModelPath(modelType, id);
            string name = id.ToString();

            model.ChangeModel(path, name, () =>
            {
                model.SetParent(_rootObj.transform);
            });

        }

        public void AddModelAnim(int modelType, string clipName)
        {
            if (!_modelList.ContainsKey(modelType))
            {
                return;
            }

            ModelObj model = _modelList[modelType];

            string path = GetAnimPath(modelType, clipName);

            model.AddClip(path, clipName);
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

        public static string GetModelPath(int modelType, int id)
        {
            return string.Format("Model/Role/{0}.ab", id);
        }

        public static string GetAnimPath(int modelType, string clipName)
        {
            return string.Format("Anim/{0}.ab", clipName);
        }
    }
}