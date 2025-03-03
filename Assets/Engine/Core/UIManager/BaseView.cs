using GameFramework.Asset;
using GameFramework.Timers;
using System.Collections.Generic;

namespace GameFramework.UI
{
    public abstract class BaseView : BaseTemple
    {


        private int _cacheTime = 10;
        private int _cacheTimeID = 0;

        private List<AssetRequest> _refPackageReqList = new List<AssetRequest>();
        private int _refPackageReqFinishNum;

        public UIZOrder uiOrder = UIZOrder.UIZOrder_Common;

        public new void Open(params object[] paramList)
        {
            if (isOpen)
            {
                ShowLayout();
                return;
            }
            _openParams = paramList;

            if (_state == UIState.Close)
            {
                _state = UIState.Loading;
                LoadPackage();
            }
            else if (_state == UIState.Caching)
            {

                ClearCacheTimer();

                _state = UIState.Open;
                ShowLayout();
                OnLayoutCreated();
            }

        }

        protected new void Close(bool immediately = false)
        {

            if (_state == UIState.Open)
            {
                OnClose();

                SetVisible(false);


                if ((_cacheTime <= 0) || immediately)
                {
                    UnLoadRes();
                    _state = UIState.Close;
                }
                else
                {
                    if (_cacheTimeID == 0)
                    {
                        _cacheTimeID = TimerManager.SetTimeout(() =>
                        {
                            UnLoadRes();
                            _state = UIState.Close;
                            ClearCacheTimer();
                        }, _cacheTime);
                    }

                    _state = UIState.Caching;
                }

            }
            else if (_state == UIState.Loading)
            {
                UnLoadRes();
                _state = UIState.Close;
            }
        }

        protected void ShowLayout()
        {
            SetVisible(true);
            UIManager.AddViewRoot(this);
        }

        private void LoadPackage()
        {
            _refPackageReqList.Clear();
            _refPackageReqFinishNum = 0;

            string bundleName = GetUIBundlePath(_packageName);
            AssetRequest req  = AssetManager.LoadAllAssetAsync(bundleName, OnLoadResFinish);
            _refPackageReqList.Add(req);
        }

        private void OnLoadResFinish(Request request)
        {
            _refPackageReqFinishNum++;

            if (_refPackageReqFinishNum != _refPackageReqList.Count)
            {
                return;
            }

            if (_root == null)
            {
                CreateLayout();
                UIManager.AddViewRoot(this);
            }
        }

        private void UnLoadRes()
        {
            DestroyLayout();
            UnLoadPackage();
        }

        private void UnLoadPackage()
        {
            if (_refPackageReqList.Count > 0)
            {

                foreach (var req in _refPackageReqList)
                {
                    AssetManager.UnLoadAssetAsync(req);
                }

                _refPackageReqList.Clear();

            }

        }

        private void ClearCacheTimer()
        {
            if (_cacheTimeID > 0)
            {
                TimerManager.ClearTimer(_cacheTimeID);
                _cacheTimeID = 0;
            }
        }
    }
}
