namespace GameFramework.Runtime
{
    internal class LoginCtrl:BaseCtrl
    {
        public LoginCtrl()
        {
            RegisterView("login_view", typeof(LoginView));
        }


        public void OpenLoginView()
        {
            OpenView("login_view", "test");
        }
    }
}
