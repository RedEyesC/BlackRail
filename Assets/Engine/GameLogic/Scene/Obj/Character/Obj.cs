using GameFramework.Scene;
using UnityEngine;


namespace GameLogic
{
    internal class Obj
    {
        protected DrawObj _drawObj;

        protected Vector2 _dir = new Vector2();
        protected Vector3 _pos = new Vector3();

        private float _targetDist = 0;
        private float _CurDist = 0;

        public float speed = 0f;

        public UnityEngine.Transform root
        {
            get
            {
                return _drawObj.root;
            }
        }

        public Obj(BodyType bodyType)
        {
            Init(bodyType);
        }

        public virtual void Init(BodyType bodyType)
        {
            _drawObj = new DrawObj(bodyType);
        }

        public virtual void Rest()
        {


        }

        public virtual void StateUpdate(float nowTime, float elapseSeconds)
        {
            UpdateMove(nowTime, elapseSeconds);
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
                _dir.Normalize();
                _drawObj.root.SetLookDir(_dir.x, 0, _dir.y);
            }

        }

        public void DoMove(float x, float y, float div)
        {

            _targetDist = div;
            _CurDist = 0;

            SetDir(x, y);
        }


        public virtual void UpdateMove(float nowTime, float elapseSeconds)
        {


            if (_targetDist > 0 && _drawObj.root)
            {
                float deltaDist = elapseSeconds * speed;

                if (_CurDist + deltaDist > _targetDist)
                {
                    _targetDist = 0;
                    _CurDist = 0;
                    deltaDist = _targetDist - _CurDist;
                    PlayAnim("Idle");
                }
                else
                {
                    _CurDist += deltaDist;
                    PlayAnim("RunFwd");
                }


                float x = _drawObj.root.position.x;
                float y = _drawObj.root.position.z;


                x += deltaDist * _dir.x;
                y += deltaDist * _dir.y;

                SetPosition(x, y);

            }
        }

        public float CalcMapHeight(float x, float z)
        {
            return SceneManager.GetHeightByRayCast(x, z);
        }


    }
}
