
using GameFramework.Moudule;

namespace GameLogic
{
    internal  class MainCtrl:BaseModule
    {
        public MainCtrl()
        {
            RegisterView("MainView", typeof(MainView));
        }


        public static void OpenMainView()
        {
            OpenView("MainView");
        }
    }
}
