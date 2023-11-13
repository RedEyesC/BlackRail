using System.Collections.Generic;

namespace GameFramework.Runtime
{

    internal abstract class BaseView : UIBase
    {
        protected object[] _openParams;

        protected int _cacheTime = 60;
        private int _cacheTimeID = 0;

        private List<int> _refPackageReqList = new List<int>();
        private int _refPackageReqFinishNum;

        protected bool isOpen
        {
            get
            {
                return _state == UIState.Open;
            }
        }



        public void Open(params object[] paramList)
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

        protected override void Close(bool immediately = false)
        {

            if (_state == UIState.Open)
            {
                base.Close(immediately);
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
                        _cacheTimeID = GlobalCenter.GetModule<TimerManager>().SetTimeout(() =>
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
        }

        private void LoadPackage()
        {
            _refPackageReqList.Clear();
            _refPackageReqFinishNum = 0;

            string path = Utils.GetUIPrefabPath(_packageName, _comName);

            int id = GlobalCenter.GetModule<AssetManager>().LoadAssetAsync(path,OnLoadResFinish);

            _refPackageReqList.Add(id);
        }

        private void OnLoadResFinish(int requestID, bool isSuccess)
        {
            _refPackageReqFinishNum++;

            if (_refPackageReqFinishNum != _refPackageReqList.Count)
            {
                return;
            }

            if (!_root)
            {
                this.CreateLayout();
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

                foreach (var id in _refPackageReqList)
                {
                    GlobalCenter.GetModule<AssetManager>().UnLoad(id);
                }

                _refPackageReqList.Clear();

            }

        }

        private void ClearCacheTimer()
        {
            if (_cacheTimeID > 0)
            {
                GlobalCenter.GetModule<TimerManager>().ClearTimer(_cacheTimeID);
                _cacheTimeID = 0;
            }
        }

        protected override void OnLayoutCreated()
        {
            base.OnLayoutCreated();

            _state = UIState.Open;

            OnOpen(_openParams);

        }

        public void SetVisible(bool val)
        {
            if (_root)
            {
                _root.SetActive(val);
            }
        }
    }
}
