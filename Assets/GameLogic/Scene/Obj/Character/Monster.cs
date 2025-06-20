
using UnityEngine;


namespace GameLogic
{

    internal class Monster : Obj
    {

        private int _monsterId;
        private float nextTime = 0;

        public float cd = 0.1f;
        public int attackRange = 9;

        public Vector2 _tempVector = new Vector2();

        public int monsterId
        {
            get { return _monsterId; }
        }


        public Monster() : base(BodyType.Monster)
        {

        }


        public void InitModel(int monsterId)
        {
            speed = 2;
            _monsterId = monsterId;
            SetModelID(1, 1001);
        }


        public void InitAI()
        {

        }


        public void UpdateAI(float nowTime, float elapseSeconds)
        {
            if (nowTime > nextTime)
            {
                nextTime = nowTime + cd;

                Role role  =  SceneCtrl.GetMainRole();

                _tempVector.Set( role.root.position.x - root.position.x,role.root.position.z - root.position.z);

                if(_tempVector.magnitude > attackRange)
                {
                    DoMove(_tempVector.x, _tempVector.y, 0.5f);
                }


            }
        }


        public override void StateUpdate(float nowTime, float elapseSeconds)
        {
            UpdateAI(nowTime, elapseSeconds);

            base.StateUpdate(nowTime, elapseSeconds);
        }
    }
}
