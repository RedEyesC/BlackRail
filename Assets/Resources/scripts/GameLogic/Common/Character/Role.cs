
using System;
using System.Collections.Generic;

namespace GameFramework.Runtime
{

    internal class Role : Obj
    {

        private Dictionary<int, ModelObj> _ModelList = new Dictionary<int, ModelObj>();

        private float _TargetX = 0;
        private float _TargetY = 0;
        private float _TargetDist = 0;
        private float _CurDist = 0;

        public float Speed = 2f;
        public float Div = 0.5f;



        public Role()
        {
            Init();
        }

        public void SetModelID(int modelType, int id)
        {

            if (!_ModelList.ContainsKey(modelType))
            {
                _ModelList.Add(modelType, new ModelObj());
            }


            ModelObj model = _ModelList[modelType];

            string path = Utils.GetRoleModelPath(id);

            model.ChangeModel(path, () =>
            {
                model.SetParent(_RootObj.transform);
            });

        }


        public void PlayAnim(string name)
        {

            foreach (KeyValuePair<int, ModelObj> kvp in _ModelList)
            {

                kvp.Value.PlayAnim(name);

            }
        }

        public void DoJoystick(int x, int y)
        {
            _TargetX = _RootObj.transform.position.x + x * Div;
            _TargetY = _RootObj.transform.position.z + y * Div;

            _TargetDist = (float)Math.Sqrt(x * x * Div * Div + y * y * Div * Div);
            _CurDist = 0;

            this.SetDir(x, y);
        }

        public void StateUpdate(float elapseSeconds, float realElapseSeconds)
        {

            if (_TargetDist > 0)
            {
                float _DeltaDist = elapseSeconds * Speed;
                _CurDist += _DeltaDist;

                float x = _RootObj.transform.position.x;
                float y = _RootObj.transform.position.z;

                if (_CurDist < _TargetDist)
                {
                    x += _DeltaDist * _Dir.x;
                    y += _DeltaDist * _Dir.y;

                    SetPosition(x, 0, y);

                    PlayAnim("run");
                }
                else
                {
                    SetPosition(_TargetX, 0, _TargetY);

                    _TargetDist = 0;
                    _TargetX = 0;
                    _TargetY = 0;
                    _CurDist = 0;

                    PlayAnim("idle");
                }
            }

            if (_RootObj.transform)
            {
                float x = _RootObj.transform.position.x;
                float y = _RootObj.transform.position.z;
                float height = CalcMapHeight(x,y);

                if(height> -999)
                {
                    SetPosition(x, height, y);
                }
               
            }
        }

        public float CalcMapHeight(float x ,float y)
        {
            return GameCenter.GetModule<ModuleCenter>().GetModule<SceneCtrl>().GetHeightByRayCast(x,y);  
        }
    }
}
