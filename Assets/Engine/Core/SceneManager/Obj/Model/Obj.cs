using UnityEngine;

namespace GameFramework.Scene
{

    internal class Obj
    {

        protected DrawObj _drawObj;

        protected Vector2 _dir = new Vector2();
        protected Vector3 _pos = new Vector3();


        public UnityEngine.Transform root
        {
            get
            {
                return _drawObj.root;
            }
        }

        public virtual void Init()
        {
            _drawObj = new DrawObj();
        }


        public virtual void Rest()
        {


        }

        public virtual void StateUpdate(float elapseSeconds, float realElapseSeconds)
        {

        }


        public void SetModelID(int modelType, int id)
        {
            _drawObj.SetModelID(modelType, id);
        }

        public void PlayAnim(string name)
        {
            _drawObj.PlayAnim(name);
        }

        public bool IsLoade()
        {
            return _drawObj.IsLoade();
        }

        public virtual void Destroy()
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

        public void SetPosition(float x, float z)
        {
            float height = CalcMapHeight(x, z);
            _pos.Set(x, height, z);
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

        public float CalcMapHeight(float x, float z)
        {
            return SceneManager.GetHeightByRayCast(x, z);
        }


    }
}
