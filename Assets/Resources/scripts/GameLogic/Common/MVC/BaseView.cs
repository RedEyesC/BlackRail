namespace GameFramework.Runtime
{

    internal abstract class BaseView : UIBase
    {
        public object[] _OpenParams;

        protected bool IsOpen
        {
            get
            {
                return this._State == "open";
            }
        }



        public void Open(params object[] paramList)
        {
            if (this.IsOpen)
            {
                this.ShowLayout();
                return;
            }
            this._OpenParams = paramList;

            if (this._State == "close")
            {

                this._State = "loading ";
                this.LoadPackage();
            }
            else if (this._State == "caching")
            {

                this.ClearCacheTimer();

                this._State = "open";
                this.ShowLayout();
                this.OnLayoutCreated();
            }

        }

        public void Close(bool? immediately){
        //    this.clearPage()
        //if (this._state === 'open')
        //    {
        //        this.doPreClose()

        //    super.close()

        //    this.onClose()

        //    if (!this.notAddMgr)
        //        {
        //            ViewMgr.getInstance().removeView(this)
        //    }

        //        this.visible = false

        //    this.fireEvent('view.closeView', this)

        //    if (this._cacheTime <= 0 || immediately)
        //        {
        //            this.unLoadRes()
        //        this._state = 'close'
        //    }
        //        else
        //        {
        //            if (!this._cacheTimeID)
        //            {
        //                this._cacheTimeID = global.Timer.setTimeout(() => {
        //                    this.unLoadRes()
        //                    this._state = 'close'
        //                    this.__clearTimer()
        //                }, this._cacheTime)
        //            }

        //            this._state = 'caching'
        //      }

        //        global.AudioMgr.playSound('ui002')
        //}
        //    else if (this._state === 'loading')
        //    {
        //        this.doPreClose()
  
        //    this.unLoadRes()
        //      this._state = 'close'
        //  }
        //}
    }

        protected void ShowLayout()
        {
            this._Root.SetActive(true);

            //TODO 有个banLayer的逻辑？？

            //if (engine.isUnity)
            //{
            //    if (this._uiPanel)
            //    {
            //        this._uiPanel.SetSortingOrder(this._uiOrder, true)
            //    }
            //}
            //else
            //{
            //    ViewMgr.getInstance().addViewRoot(this)
            //}
        }

        protected void LoadPackage() {
        
        
        
        }
        private void ClearCacheTimer()
        {
            //if (this._cacheTimeID)
            //{
            //    global.Timer.clearTimer(this._cacheTimeID)
            //    this._cacheTimeID = null
            //}
        }

        protected override void OnLayoutCreated()
        {
            base.OnLayoutCreated();

            this.OnOpen(this._OpenParams);

        }
    }
}
