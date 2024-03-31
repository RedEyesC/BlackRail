
using UnityEngine;

namespace GameFramework.Runtime
{
    internal abstract class UIBase
    {
        protected enum UIState
        {
            Open,
            Close,
            Caching,
            Loading
        }

        protected enum Layer
        {
            UI = 5,
        }

        protected string _packageName;
        protected string _comName;
        protected Layer _layerName = Layer.UI;

        protected UnityEngine.GameObject _root;
        protected void SetLayer(Layer layer)
        {
            _layerName = layer;
            SetLayerInternal();
        }

        private void SetLayerInternal()
        {
            _root.layer = (int)_layerName;
        }

        protected void CreateLayout()
        {

            if (!_root)
            {
                string bundleName = Utils.GetUIBundlePath(_packageName);
                _root = GlobalCenter.GetModule<UIManager>().CreateLayout(bundleName, _comName);
            }

            OnLayoutCreated();

        }

        protected void DestroyLayout()
        {
            if (_root)
            {
                GlobalCenter.GetModule<UIManager>().DestroyLayout(_root);
                _root = null;
            }
        }

        protected virtual void OnLayoutCreated()
        {
            SetLayerInternal();
        }

        protected UnityEngine.Transform GetChild(string name)
        {

            if (_root != null)
            {
                UnityEngine.Transform obj = _root.transform;

                string[] nameList = name.Split('/');

                for (int i = 0; i < nameList.Length; i++)
                {
                    obj = obj.Find(nameList[i]);
                }

                return obj;
            }

            return null;
        }

        public void SetVisible(bool val)
        {
            if (_root)
            {
                _root.SetActive(val);
            }
        }

        public Transform GetRoot()
        {
            return _root.transform;
        }

    }
}
