
using UnityEngine;

namespace GameFramework.Runtime
{

    internal abstract class BaseTemple : UIBase
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

        public void SetParent(UnityEngine.GameObject parent)
        {
            if (isOpen)
            {
                this._parent = parent;
                parent.transform.AddChild(_root.transform);
            }
        }


        public void SetPosition( Vector3  pos)
        {
            if(_root != null)
            {
                _root.transform.position = pos;
            }
        }

        public void SetLocalPosition(Vector3 pos)
        {
            if (_root != null)
            {
                _root.transform.localPosition = pos;
            }
        }

        public void SetLocalEulerAngles(Vector3 pos)
        {
            if (_root != null)
            {
                _root.transform.localEulerAngles = pos;
            }
        }
    }
}
