namespace GameFramework.Runtime
{
    //UIState = 'open' | 'caching' | 'loading' | 'close'
    internal abstract class UIBase
    {
        protected  string _PackageName;
        protected  string _ComName;
        protected string _State = "close";
        protected int _LayerName;

        protected UnityEngine.GameObject _Root;
   

        protected abstract void OnOpen(params object[] paramList);

        protected virtual void Close() { }

        protected abstract void OnClose();

        public void SetLayer(int layer)
        {
            _LayerName = layer;
            SetLayerInternal();
        }

        private void SetLayerInternal()
        {
            _Root.layer = _LayerName;
        }

        protected void CreateLayout(){

            if (!_Root)
            {
                _Root = GlobalCenter.GetModule<UIManager>().CreateLayout(_PackageName, _ComName);
                OnLayoutCreated();
            }
            else
            {
                OnLayoutCreated();
            }

        }

        protected virtual void OnLayoutCreated()
        {
            SetLayerInternal();
        }


    }
}
