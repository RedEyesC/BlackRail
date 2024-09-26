
using GameFramework.Common;
using GameFramework.Scene;
using GameLogic;
using UnityEngine;

namespace GameFramework.Input
{
    internal class InputManager : GameModule
    {
        public new int priority = 6;

        public override void Destroy()
        {
            
        }

        public override void Start()
        {
            
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            //TODO 先临时用这个，等空重构整个输入控制类
            Role mainRole = SceneCtrl.mainRole;

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

            if (TempX != 0 || TempY != 0)
            {
                mainRole.DoJoystick(TempX, TempY);
            }
        }
    }
}
