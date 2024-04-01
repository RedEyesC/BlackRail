using UnityEngine;

namespace GameFramework.Runtime
{
    internal class InputCtrl : BaseCtrl
    {
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            Role mainRole = GameCenter.GetModule<ModuleCenter>().GetModule<SceneCtrl>().mainRole;

            int TempX = 0;
            int TempY = 0;

            if (UnityEngine.Input.GetKey(KeyCode.A))
            {
                TempX = -1;
            }
            else if (UnityEngine.Input.GetKey(KeyCode.D))
            {
                TempX = 1;
            }


            if (UnityEngine.Input.GetKey(KeyCode.W))
            {
                TempY = 1;
            }
            else if (UnityEngine.Input.GetKey(KeyCode.S))
            {
                TempY = -1;
            }

            if(TempX != 0|| TempY != 0)
            {
                mainRole.DoJoystick(TempX, TempY);
            }
         
        }
    }
}
