namespace GameFramework.UI
{
    internal class LoginView : BaseView
    {

        public LoginView()
        {
            _packageName = "Login";
            _comName = "LoginView";
        }

        protected override void OnClose()
        {
            
        }

        protected override void OnOpen(params object[] paramList)
        {

            GetChild<GButton>("btn_start").AddClickCallback((float x, float y) =>
            {
                Close();
            });

        }
     
    }
}
