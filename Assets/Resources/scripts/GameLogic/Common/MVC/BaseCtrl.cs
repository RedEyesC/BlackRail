

using System;
using System.Collections.Generic;

namespace GameFramework.Runtime
{
    internal abstract class BaseCtrl
    {

        private Dictionary<string, Type> _ViewDefines = new Dictionary<string, Type>();
        private Dictionary<string, BaseView> _ViewMap = new Dictionary<string, BaseView>();

        protected void RegisterView(string viewName, Type viewType)
        {
            if (!_ViewDefines.ContainsKey(viewName))
            {
                _ViewDefines.Add(viewName, viewType);
            }
        }


        protected void OpenView(string viewName, params object[] paramList)
        {
            BaseView view = GetView(viewName);
            if (view != null)
            {
                view.Open(paramList);
            }
        }

        public BaseView GetView(string name)
        {
            if (_ViewDefines.ContainsKey(name))
            {
                if (!_ViewMap.ContainsKey(name))
                {
                    Type type = _ViewDefines[name];
                    BaseView view = (BaseView)Activator.CreateInstance(type);
                    _ViewMap[name] = view;
                }

                return _ViewMap[name];
            }
            else
            {
                return null;
            }


        }

    }

}



