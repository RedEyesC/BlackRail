
using GameFramework.Input;
using GameFramework.Moudule;

namespace GameLogic
{
    internal  class SysSettingCtrl:BaseModule
    {
        public SysSettingCtrl()
        {
           
        }


        public static void InitSysSetting()
        {
            //初始化键位操作
            InputManager.CreateDigitalAxis("Action", "Horizontal", UnityEngine.KeyCode.A, UnityEngine.KeyCode.D, 3, 3);
            InputManager.CreateDigitalAxis("Action", "Vertical", UnityEngine.KeyCode.S, UnityEngine.KeyCode.W, 3, 3);

            InputManager.SetPlayerScheme("Action");

        }

    }
}
