

using UnityEngine;

namespace GameFramework.Runtime
{
    internal class LoginView : BaseView
    {

        public LoginView()
        {
            _PackageName = "ui_login";
            _ComName = "login_view";
        }

        protected override void OnClose()
        {
            
        }

        protected override void OnOpen(params object[] paramList)
        {
            Debug.Log("test");
        }
        
     
    }
}
