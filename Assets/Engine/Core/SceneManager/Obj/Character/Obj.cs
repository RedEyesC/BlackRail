using UnityEngine;

namespace GameFramework.Scene
{

    internal class Obj
    {

        protected DrawObj _drawObj;

        protected Vector2 _dir = new Vector2();
        protected Vector3 _pos = new Vector3();


        public virtual void Init()
        {
            _drawObj = new DrawObj();
        }


        public void SetModelID(int modelType, int id)
        {
            _drawObj.SetModelID(modelType, id);
        }


        public void PlayAnim(string name)
        {
            _drawObj.PlayAnim(name);
        }

        protected virtual void Destroy()
        {
            if (_drawObj != null)
            {
                _drawObj.Rest();
                _drawObj = null;
            }
        }


        public void SetPosition(float x, float y, float z)
        {
            _pos.Set(x, y, z);
            _drawObj.root.position = _pos;
        }


        public void SetDir(float x, float y)
        {
            if (x != 0 || y != 0)
            {
                _dir.Set(x, y);
                _drawObj.root.SetLookDir(_dir.x, 0, _dir.y);
            }

        }
    }
}
