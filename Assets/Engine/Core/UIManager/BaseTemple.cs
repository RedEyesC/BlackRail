
using UnityEngine;

namespace GameFramework.Runtime
{

    internal abstract class BaseTemple : BaseUI
    {
        protected object[] _openParams;
        protected UnityEngine.GameObject _parent;
        protected UIState _state = UIState.Close;


        public void Open(params object[] paramList)
        {
            if (!isOpen)
            {
                _openParams = paramList;
                CreateLayout();
            }
        }

        protected abstract void OnOpen(params object[] paramList);

        protected override void OnLayoutCreated()
        {
            base.OnLayoutCreated();

            _state = UIState.Open;

            OnOpen(_openParams);

        }

        public void Close()
        {

            if (isOpen)
            {
                OnClose();
                DestroyLayout();

                _state = UIState.Close;
            }
        }

        protected abstract void OnClose();

        protected bool isOpen
        {
            get
            {
                return _state == UIState.Open;
            }
        }

    }
}
