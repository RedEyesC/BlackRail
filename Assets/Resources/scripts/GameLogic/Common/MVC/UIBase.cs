namespace GameFramework.Runtime
{
    //UIState = 'open' | 'caching' | 'loading' | 'close'
    internal abstract class UIBase
    {
        protected string _PackageName;
        protected string _ComName;
        protected string _State;
        protected int _LayerName;

        protected UnityEngine.GameObject _Root;
   

        protected abstract void OnOpen(params object[] paramList);

        protected abstract void OnClose();

        public void SetLayer(int layer)
        {
            this._LayerName = layer;
            this.SetLayerInternal();
        }

        private void SetLayerInternal()
        {
            this._Root.layer = this._LayerName;
        }

        protected void CreateLayout(){

            if (!this._Root)
            {
                this._Root = GlobalCenter.GetModule<UIManager>().CreateLayout(this._PackageName, this._ComName);
                this.OnLayoutCreated();
            }
            else
            {
                this.OnLayoutCreated();
            }

        }

        protected virtual void OnLayoutCreated()
        {
            this.SetLayerInternal();
        }
    }
}
