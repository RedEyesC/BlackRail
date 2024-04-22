namespace GameFramework.Runtime
{
    internal class LoginCtrl:BaseCtrl
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
