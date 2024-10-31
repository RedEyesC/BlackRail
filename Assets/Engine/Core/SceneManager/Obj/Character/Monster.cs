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
            if (_stateMachine == null)
            {
                _stateMachine = new StateMachine();
                _stateMachine.AddState(new IdleState(this));
                _stateMachine.AddState(new MoveState(this));
            }
        }


        public void UpdateAI(float elapseSeconds, float realElapseSeconds)
        {
            _stateMachine?.Update(elapseSeconds, realElapseSeconds);
        }


        public override void StateUpdate(float elapseSeconds, float realElapseSeconds)
        {
            UpdateAI(elapseSeconds, realElapseSeconds);
        }
    }
}
