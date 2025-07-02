using GameFramework.AppLoop;
using GameFramework.UI;

namespace GameLogic
{
   
    class RoleTemplate : BaseTemple
    {
        private BaseView _parent;

        protected override void OnClose()
        {
            
        }

        protected override void OnOpen(params object[] paramList)
        {
            _parent = paramList[0] as BaseView;

            GetChild<GButton>("btn_go").AddClickCallback((float x, float y) =>
            {
                AppLoopManager.ChangeState("Loading");
                _parent.Close();
            });
        }
    }
}
