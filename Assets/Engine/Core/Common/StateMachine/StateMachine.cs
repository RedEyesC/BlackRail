using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameFramework.Runtime
{

    public class StateMachine
    {
        private Dictionary<string, StateBase> _StateMap = new Dictionary<string, StateBase>();
        private StateBase _CurState = null;

        public void Start()
        {

        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (_CurState != null)
            {
                _CurState.StateUpdate(elapseSeconds, realElapseSeconds);
            }
        }

        public void AddState(StateBase state)
        {
            _StateMap.Add(state.GetID(), state);
        }

        public void ChangeState(string id, params object[] paramList)
        {
 
            StateBase newState = _StateMap[id];
            if (newState != null)
            {
                if (_CurState != null)
                {
                    _CurState.StateQuit(paramList);
                }

                _CurState = newState;
                _CurState.StateEnter(paramList);
            }
        }

        public void Destroy(object[] paramList)
        {
            if(_CurState != null)
            {
                _CurState.StateQuit(paramList);
            }
            _StateMap.Clear();
            _CurState = null;
        }
    }
}
