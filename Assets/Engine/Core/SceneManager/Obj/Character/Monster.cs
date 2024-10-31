using GameFramework.Common;

namespace GameFramework.Scene
{

    internal class Monster : Obj
    {

        private int _monsterId;

        private StateMachine _stateMachine;

        public int monsterId
        {
            get { return _monsterId; }
        }

        public Monster()
        {

        }


        public void Init(int monsterId)
        {
            _monsterId = monsterId;

            InitAI();

            base.Init();
        }


        public void InitAI()
        {
            //一个简单的基于状态机的ai
            if (_stateMachine == null)
            {
                _stateMachine = new StateMachine();
                _stateMachine.AddState(new IdleState(this));
                _stateMachine.AddState(new MoveState(this));
            }
        }


        public void UpdateAI(float nowTime, float elapseSeconds)
        {
            _stateMachine?.Update(nowTime, elapseSeconds);
        }


        public override void StateUpdate(float nowTime, float elapseSeconds)
        {
            UpdateAI(nowTime, elapseSeconds);
        }
    }
}
