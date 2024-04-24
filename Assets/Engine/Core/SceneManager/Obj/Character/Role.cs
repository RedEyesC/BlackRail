
using GameFramework.Common;
using System;
using System.Collections.Generic;

namespace GameFramework.Scene
{

    internal class Role : Obj
    {

        private Dictionary<int, ModelObj> _modelList = new Dictionary<int, ModelObj>();

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

        public void SetModelID(int modelType, int id)
        {

            if (!_modelList.ContainsKey(modelType))
            {
                _modelList.Add(modelType, new ModelObj());
            }


            ModelObj model = _modelList[modelType];

            string path = Utils.GetRoleModelPath(id);

            model.ChangeModel(path, () =>
            {
                model.SetParent(_rootObj.transform);
            });

        }


        public void PlayAnim(string name)
        {

            foreach (KeyValuePair<int, ModelObj> kvp in _modelList)
            {

                kvp.Value.PlayAnim(name);

            }
        }

        public void DoJoystick(int x, int y)
        {
            _targetX = _rootObj.transform.position.x + x * div;
            _targetY = _rootObj.transform.position.z + y * div;

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

                float x = _rootObj.transform.position.x;
                float y = _rootObj.transform.position.z;

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

            if (_rootObj.transform)
            {
                float x = _rootObj.transform.position.x;
                float y = _rootObj.transform.position.z;
                float height = CalcMapHeight(x,y);

                if(height> -999)
                {
                    SetPosition(x, height, y);
                }
               
            }
        }

        public float CalcMapHeight(float x ,float y)
        {
            return 0;
            //TODO
            //return ModuleManager.GetModule<SceneCtrl>().GetHeightByRayCast(x,y);  
        }
    }
}
