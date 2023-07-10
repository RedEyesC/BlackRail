
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


        protected string _PackageName;
        protected  string _ComName;
        protected  UIState  _State = UIState.Close;
        protected Layer _LayerName = Layer.UI;

        protected UnityEngine.GameObject _Root;
   

        protected abstract void OnOpen(params object[] paramList);

        protected virtual void Close() { }

        protected abstract void OnClose();

        protected void SetLayer(Layer layer)
        {
            _LayerName = layer;
            SetLayerInternal();
        }

        private void SetLayerInternal()
        {
            _Root.layer = (int)_LayerName ;
        }

        protected void CreateLayout(){

            if (!_Root)
            {
                string path = Utils.GetUIPrefabPath(_PackageName, _ComName);
                _Root = GlobalCenter.GetModule<UIManager>().CreateLayout(path); 
            }

            OnLayoutCreated();
 

        }

        protected virtual void OnLayoutCreated()
        {
            SetLayerInternal();
        }


    }
}
