
using GameFramework.Moudule;

namespace GameLogic
{
    internal class LoginCtrl:BaseModule
    {
        public LoginCtrl()
        {
            RegisterView("loginView", typeof(LoginView));
        }


        public void OpenLoginView()
        {
            OpenView("loginView", "test");
        }
    }
}
