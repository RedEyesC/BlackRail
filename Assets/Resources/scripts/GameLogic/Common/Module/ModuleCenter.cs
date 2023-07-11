using System;
using System.Collections.Generic;

namespace GameFramework.Runtime
{
    internal class ModuleCenter : GameModule
    {

        private Dictionary<string, BaseCtrl> _CtrlMap = new Dictionary<string, BaseCtrl>();

        private List<Type> _CtrlList = new List<Type>
        {
            typeof(LoginCtrl),
        };


        public override void Destroy()
        {
            _CtrlMap.Clear();
        }

        public override void Start()
        {
            
            foreach ( Type ctrl in _CtrlList)
            {
         
                BaseCtrl Cls = (BaseCtrl)Activator.CreateInstance(ctrl);      
                _CtrlMap[ctrl.Name] = Cls;

            }

        }

        public T GetModule<T>() where T : BaseCtrl
        {
            Type interfaceType = typeof(T);
            if (_CtrlMap.ContainsKey(interfaceType.Name))
            {
                return _CtrlMap[interfaceType.Name] as T;
            }

            return null;
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {

        }



    }

}
