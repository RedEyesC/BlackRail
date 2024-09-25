using GameFramework.AppLoop;
using GameFramework.UI;
using UnityEngine;

namespace GameLogic
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

            GButton start = GetChild<GButton>("btn_start");
            start.text = Utils.Text(1);
            start.AddClickCallback((float x, float y) =>
            {
                AppLoopManager.ChangeState("Loading");
                Close();
            });
        }
     
    }
}
