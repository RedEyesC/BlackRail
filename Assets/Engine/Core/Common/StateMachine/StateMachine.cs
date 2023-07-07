using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameFramework.Runtime
{

    public class StateMachine
    {
        private Dictionary<string, StateBase> mStateMap = new Dictionary<string, StateBase>();
        private StateBase mCurState = null;

        public void Start()
        {
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (mCurState != null)
            {
                mCurState.StateUpdate(elapseSeconds, realElapseSeconds);
            }
        }

        public void AddState(StateBase state)
        {
            mStateMap.Add(state.GetID(), state);
        }

        public void ChangeState(string id)
        {
 
            StateBase newState = mStateMap[id];
            if (newState != null)
            {
                if (mCurState != null)
                {
                    mCurState.StateQuit();
                }

                mCurState = newState;
                mCurState.StateEnter();
            }
        }
    }
}
