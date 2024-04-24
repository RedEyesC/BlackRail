using UnityEngine;

namespace GameFramework.Scene
{

    internal class Obj
    {
        protected UnityEngine.GameObject _rootObj;

        protected Vector2 _dir = new Vector2();

        public UnityEngine.Transform root
        {
            get
            {
                return _rootObj.transform;
            }
        }


        public virtual void Init()
        {

            _rootObj = new GameObject();
            SceneManager.AddToObjRoot(_rootObj.transform);

        }


        protected virtual void Destroy()
        {
            if (_rootObj)
            {
                SceneManager.DestroyLayout(_rootObj);
                _rootObj = null;
            }
        }

        public void SetPosition(float x, float y, float z)
        {

            _rootObj.transform.position = new Vector3(x, y, z);
        }

        public void SetDir(float x, float y)
        {
            if (x != 0 || y != 0)
            {
                _dir.Set(x, y);

                if (_rootObj && _rootObj.transform)
                {

                    _rootObj.transform.SetLookDir(_dir.x, 0, _dir.y);

                }

            }

        }
    }
}
