using System;
using System.Collections.Generic;

namespace GameFramework.Runtime
{
    internal abstract class BaseCtrl
    {

        private static Dictionary<string, Type> _viewDefines = new Dictionary<string, Type>();
        private static Dictionary<string, BaseView> _viewMap = new Dictionary<string, BaseView>();

        protected void RegisterView(string viewName, Type viewType)
        {
            if (!_viewDefines.ContainsKey(viewName))
            {
                _viewDefines.Add(viewName, viewType);
            }
        }


        protected static void OpenView(string viewName, params object[] paramList)
        {
            BaseView view = GetView(viewName);
            if (view != null)
            {
                view.Open(paramList);
            }
        }

        public static BaseView GetView(string name)
        {
            if (_viewDefines.ContainsKey(name))
            {
                if (!_viewMap.ContainsKey(name))
                {
                    Type type = _viewDefines[name];
                    BaseView view = (BaseView)Activator.CreateInstance(type);
                    _viewMap[name] = view;
                }

                return _viewMap[name];
            }
            else
            {
                return null;
            }


        }

    }

}



