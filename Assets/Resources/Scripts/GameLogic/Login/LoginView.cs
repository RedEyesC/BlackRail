
namespace GameFramework.Runtime
{
    internal class LoginView : BaseView
    {

        public LoginView()
        {
            _packageName = "ui_login";
            _comName = "login_view";
        }

        protected override void OnClose()
        {
            
        }

        protected override void OnOpen(params object[] paramList)
        {

            GetChild<GButton>("btn_start").AddClickEventListener((float x, float y) =>
            {
                this.Close();
            });

           GButton  a = GetChild<GButton>("111");
        }
     
    }
}
