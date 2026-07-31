using GameFramework.Input;

namespace GameLogic
{
    internal class SysSettingCtrl : BaseModule
    {
        public SysSettingCtrl() { }

        public static void InitSysSetting()
        {
            //初始化键位操作
            InputManager.CreateDigitalAxis("Action", "Horizontal", UnityEngine.KeyCode.A, UnityEngine.KeyCode.D, 3, 3, true);
            InputManager.CreateDigitalAxis("Action", "Vertical", UnityEngine.KeyCode.W, UnityEngine.KeyCode.S, 3, 3, true);
            InputManager.CreateButton("Action", "Run", UnityEngine.KeyCode.LeftShift);

            InputManager.SetPlayerScheme("Action");
        }
    }
}
