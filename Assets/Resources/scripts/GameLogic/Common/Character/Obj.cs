using UnityEngine;

namespace GameFramework.Runtime
{

    internal class Obj
    {
        protected UnityEngine.GameObject _RootObj;

        protected Vector2 _Dir = new Vector2();

        public UnityEngine.Transform Root
        {
            get
            {
                return _RootObj.transform;
            }
        }


        public virtual void Init()
        {

            _RootObj = new GameObject();
            GlobalCenter.GetModule<CameraManager>().AddToObjRoot(_RootObj.transform);

        }


        protected virtual void Destroy()
        {
            if (_RootObj)
            {
                GlobalCenter.GetModule<CameraManager>().DestroyLayout(_RootObj);
                _RootObj = null;
            }
        }

        public void SetPosition(float x, float y, float z)
        {

            _RootObj.transform.position = new Vector3(x, y, z);
        }

        public void SetDir(float x, float y)
        {
            if (x != 0 || y != 0)
            {
                _Dir.Set(x, y);

                if (_RootObj && _RootObj.transform)
                {

                    _RootObj.transform.SetLookDir(_Dir.x, 0, _Dir.y);

                }

            }

        }
    }
}
