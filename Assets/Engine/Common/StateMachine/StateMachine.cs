using System.Collections.Generic;

namespace GameFramework.Common
{

    public class StateMachine
    {
        private Dictionary<string, StateBase> _stateMap = new Dictionary<string, StateBase>();
        private StateBase _curState = null;

        public void Start()
        {

        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (_curState != null)
            {
                _curState.StateUpdate(elapseSeconds, realElapseSeconds);
            }
        }

        public void AddState(StateBase state)
        {
            _stateMap.Add(state.GetID(), state);
        }

        public void ChangeState(string id, params object[] paramList)
        {
 
            StateBase newState = _stateMap[id];
            if (newState != null)
            {
                if (_curState != null)
                {
                    _curState.StateQuit(paramList);
                }

                _curState = newState;
                _curState.StateEnter(paramList);
            }
        }

        public void Destroy(object[] paramList)
        {
            if(_curState != null)
            {
                _curState.StateQuit(paramList);
            }
            _stateMap.Clear();
            _curState = null;
        }
    }
}
