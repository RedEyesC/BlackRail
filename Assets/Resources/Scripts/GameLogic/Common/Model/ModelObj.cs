

using System;
using UnityEngine;

namespace GameFramework.Runtime
{
    internal class ModelObj
    {
        private string _path;
        private Action _loadCallBack;
        private int _reqId;
        private GameObject _obj;


        private void OnLoadResFinish(int requestID, bool isSuccess)
        {
            if (isSuccess)
            {
                //_obj = GlobalCenter.GetModule<AssetManager>().CreateAsset(_path);
            }

            if (_loadCallBack != null)
            {
                _loadCallBack();
            }
        }

        public void ChangeModel(string path, System.Action cb = null)
        {
            if (path == _path)
            {
                return;
            }

            if (_obj != null)
            {
                //GlobalCenter.GetModule<AssetManager>().DestoryAsset(_obj);
            }

            _path = path;
            _loadCallBack = cb;

            //_reqId = GlobalCenter.GetModule<AssetManager>().LoadAssetAsync(path, OnLoadResFinish);


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
