using UnityEngine;

namespace GameFramework.Runtime
{
  
    internal class Obj
    {
        protected UnityEngine.GameObject _RootObj;


        public UnityEngine.Transform Root
        {
            get
            {
                return _RootObj.transform;
            }
        }


        public virtual void Init() { 

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

        public void SetPosition(float x, float y,float z)
        {

            _RootObj.transform.position = new Vector3(x, y, z);
        }
    }
}
