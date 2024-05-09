using System;

namespace GameFramework.Scene
{

    internal class Role : Obj
    {

        private float _targetX = 0;
        private float _targetY = 0;
        private float _targetDist = 0;
        private float _CurDist = 0;

        public float speed = 2f;
        public float div = 0.5f;


        public Role()
        {
            Init();
        }


        public void DoJoystick(int x, int y)
        {
            _targetX = _drawObj.root.position.x + x * div;
            _targetY = _drawObj.root.transform.position.z + y * div;

            _targetDist = (float)Math.Sqrt(x * x * div * div + y * y * div * div);
            _CurDist = 0;

            this.SetDir(x, y);
        }

        public void StateUpdate(float elapseSeconds, float realElapseSeconds)
        {

            if (_targetDist > 0)
            {
                float deltaDist = elapseSeconds * speed;
                _CurDist += deltaDist;

                float x = _drawObj.root.position.x;
                float y = _drawObj.root.position.z;

                if (_CurDist < _targetDist)
                {
                    x += deltaDist * _dir.x;
                    y += deltaDist * _dir.y;

                    SetPosition(x, 0, y);

                    PlayAnim("run");
                }
                else
                {
                    SetPosition(_targetX, 0, _targetY);

                    _targetDist = 0;
                    _targetX = 0;
                    _targetY = 0;
                    _CurDist = 0;

                    PlayAnim("idle");
                }
            }

            if (_drawObj.root)
            {
                float x = _drawObj.root.position.x;
                float y = _drawObj.root.position.z;
                float height = CalcMapHeight(x,y);

                if(height> -999)
                {
                    SetPosition(x, height, y);
                }
               
            }
        }

        public float CalcMapHeight(float x ,float y)
        {
           return SceneManager.GetHeightByRayCast(x,y);  
        }
    }
}
