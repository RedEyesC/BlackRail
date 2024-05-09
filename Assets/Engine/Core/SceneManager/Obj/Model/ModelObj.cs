

using GameFramework.Asset;
using System;
using UnityEngine;

namespace GameFramework.Scene
{
    internal class ModelObj
    {
        private string _path;
        private string _name;
        private Action _loadCallBack;
        private AssetRequest _req;
        private GameObject _obj;


        private void OnLoadResFinish(Request req)
        {
            if (req.isDone)
            {
                GameObject Obj = AssetManager.GetAssetObjWithType<GameObject>(_path, _name);
                _obj = GameObject.Instantiate<GameObject>(Obj);
            }

            if (_loadCallBack != null)
            {
                _loadCallBack();
            }

        }

        public void ChangeModel(string modelPath, string modelName, System.Action cb = null)
        {
            if (_path == modelPath && _name == modelName)
            {
                return;
            }

            if (_obj != null)
            {
                AssetManager.UnLoadAssetAsync(_req);
                SceneManager.DestroyLayout(_obj);
            }

            _path = modelPath;
            _name = modelName;

            _loadCallBack = cb;

            _req = AssetManager.LoadAssetAsync(_path, _name, OnLoadResFinish);

        }

        public void SetParent(Transform parent)
        {
            parent.AddChild(this._obj.transform);
        }


        public void PlayAnim(string name)
        {
            if(_obj != null)
            {
                _obj.GetComponent<Animator>().Play(name);
            }
        }



    }
}
