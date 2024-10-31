using GameFramework.UI;
using System;

namespace GameFramework.Moudule
{
    internal class BaseModule
    {

        public virtual void Update(float nowTime, float elapseSeconds)
        {

        }


        public static void RegisterView(string viewName, Type viewType)
        {
            UIManager.RegisterView(viewName, viewType);
        }


        public static void OpenView(string viewName, params object[] paramList)
        {
            UIManager.OpenView(viewName, paramList);
        }


        public static BaseView GetView(string name)
        {
            return UIManager.GetView(name);
        }


    }

}



