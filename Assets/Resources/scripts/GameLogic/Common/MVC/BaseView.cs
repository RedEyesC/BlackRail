using System.Collections.Generic;

namespace GameFramework.Runtime
{

    internal abstract class BaseView : UIBase
    {
        protected object[] _OpenParams;

        protected int _CacheTime = 60;
        private int _CacheTimeID = 0;

        private List<int> _RefPackageReqList = new List<int>();
        private int _RefPackageReqFinishNum;

        protected bool IsOpen
        {
            get
            {
                return _State == UIState.Open;
            }
        }



        public void Open(params object[] paramList)
        {
            if (IsOpen)
            {
                ShowLayout();
                return;
            }
            _OpenParams = paramList;

            if (_State == UIState.Close)
            {

                _State = UIState.Loading;
                LoadPackage();
            }
            else if (_State == UIState.Caching)
            {

                ClearCacheTimer();

                _State = UIState.Open;
                ShowLayout();
                OnLayoutCreated();
            }

        }

        protected override void Close(bool immediately = false)
        {

            if (_State == UIState.Open)
            {
                base.Close(immediately);
                OnClose();

                SetVisible(false);


                if ((_CacheTime <= 0) || immediately)
                {
                    UnLoadRes();
                    _State = UIState.Close;
                }
                else
                {
                    if (_CacheTimeID == 0)
                    {
                        _CacheTimeID = GlobalCenter.GetModule<TimerManager>().SetTimeout(() =>
                        {
                            UnLoadRes();
                            _State = UIState.Close;
                            ClearCacheTimer();
                        }, _CacheTime);
                    }

                    _State = UIState.Caching;
                }

            }
            else if (_State == UIState.Loading)
            {
                UnLoadRes();
                _State = UIState.Close;
            }
        }

        protected void ShowLayout()
        {
            SetVisible(true);
        }

        private void LoadPackage()
        {
            _RefPackageReqList.Clear();
            _RefPackageReqFinishNum = 0;

            string path = Utils.GetUIPrefabPath(_PackageName, _ComName);

            int id = GlobalCenter.GetModule<AssetManager>().LoadAssetAsync(path,OnLoadResFinish);

            _RefPackageReqList.Add(id);
        }

        private void OnLoadResFinish(int requestID, bool isSuccess)
        {
            _RefPackageReqFinishNum++;

            if (_RefPackageReqFinishNum != _RefPackageReqList.Count)
            {
                return;
            }

            if (!_Root)
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

            if (_RefPackageReqList.Count > 0)
            {

                foreach (var id in _RefPackageReqList)
                {
                    GlobalCenter.GetModule<AssetManager>().UnLoad(id);
                }

                _RefPackageReqList.Clear();

            }

        }

        private void ClearCacheTimer()
        {
            if (_CacheTimeID > 0)
            {
                GlobalCenter.GetModule<TimerManager>().ClearTimer(_CacheTimeID);
                _CacheTimeID = 0;
            }
        }

        protected override void OnLayoutCreated()
        {
            base.OnLayoutCreated();

            _State = UIState.Open;

            OnOpen(_OpenParams);

        }

        public void SetVisible(bool val)
        {
            if (_Root)
            {
                _Root.SetActive(val);
            }
        }
    }
}
