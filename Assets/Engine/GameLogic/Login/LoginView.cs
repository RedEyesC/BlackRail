using GameFramework.AppLoop;
using GameFramework.Asset;
using UnityEngine;

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

            GetChild<GButton>("btn_close").AddClickCallback((float x, float y) =>
            {
                Application.Quit();
            });


            GetChild<GButton>("btn_start").AddClickCallback((float x, float y) =>
            {
                AppLoopManager.ChangeState("Loading");
                Close();
            });
        }
     
    }
}
