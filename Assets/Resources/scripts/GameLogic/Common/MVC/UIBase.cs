
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
        protected  string _comName;
        protected  UIState  _state = UIState.Close;
        protected Layer _layerName = Layer.UI;

        protected UnityEngine.GameObject _root;
   

        protected abstract void OnOpen(params object[] paramList);

        protected virtual void Close(bool immediately) { }

        protected abstract void OnClose();

        protected void SetLayer(Layer layer)
        {
            _layerName = layer;
            SetLayerInternal();
        }

        private void SetLayerInternal()
        {
            _root.layer = (int)_layerName ;
        }

        protected void CreateLayout(){

            if (!_root)
            {
                string path = Utils.GetUIPrefabPath(_packageName, _comName);
                _root = GlobalCenter.GetModule<UIManager>().CreateLayout(path); 
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
                return _root.transform.Find(name);
            }

            return null;
        }


    }
}
