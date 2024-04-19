namespace GameFramework.UI
{
    public enum UIState
    {
        Open,
        Close,
        Caching,
        Loading
    }

    public abstract class BaseUI
    {

        protected enum Layer
        {
            UI = 5,
        }

        protected string _packageName;
        protected string _comName;
        protected Layer _layerName = Layer.UI;

        protected GComponent _root;
        protected void SetLayer(Layer layer)
        {
            _layerName = layer;
            SetLayerInternal();
        }

        private void SetLayerInternal()
        {
            _root.SetLayer((int)_layerName);
        }

        protected void CreateLayout()
        {

            if (_root == null)
            {
                string bundleName = GetUIBundlePath(_packageName);
                _root = UIManager.CreateLayout(bundleName, _comName);
            }

            OnLayoutCreated();

        }

        protected void DestroyLayout()
        {
            if (_root != null)
            {
                UIManager.DestroyLayout(_root.obj);
                _root = null;
            }
        }

        protected virtual void OnLayoutCreated()
        {
            SetLayerInternal();
        }

        protected T GetChild<T>(string name) where T : GObject
        {
            if (_root != null)
            {
                GComponent obj = _root;
                string[] nameList = name.Split('/');
                int count = nameList.Length - 1;
                for (int i = 0; i < count; i++)
                {
                    obj = obj.GetChild(nameList[i]) as GComponent;
                }

                return obj.GetChild(nameList[count]) as T;

            }

            return null;
        }

        public void SetVisible(bool val)
        {
            if (_root != null)
            {
                _root.SetActive(val);
            }
        }

        public string GetUIBundlePath(string pkgName)
        {
            return string.Format("UI/{0}.ab", pkgName);
        }

        public GComponent GetRoot()
        {
            return _root;
        }

    }
}
