

using System;
using UnityEngine;

namespace GameFramework.Runtime
{
    internal class ModelObj
    {
        private string _Path;
        private Action _LoadCallBack;
        private int _ReqId;
        private GameObject _Obj;


        private void OnLoadResFinish(int requestID, bool isSuccess)
        {
            if (isSuccess)
            {
                _Obj = GlobalCenter.GetModule<AssetManager>().CreateAsset(_Path);
            }

            if(_LoadCallBack != null)
            {
                _LoadCallBack();
            }
        }

        public void ChangeModel(string path, System.Action cb = null)
        {
            if(path == _Path)
            {
                return;
            }

            if(_Obj != null)
            {
                GlobalCenter.GetModule<AssetManager>().DestoryAsset(_Obj);
            }

            _Path = path;
            _LoadCallBack = cb;

            _ReqId = GlobalCenter.GetModule<AssetManager>().LoadAssetAsync(path, OnLoadResFinish);


        }

       public void SetParent(Transform parent)
        {
            parent.AddChild(this._Obj.transform);
        }
    }
}
