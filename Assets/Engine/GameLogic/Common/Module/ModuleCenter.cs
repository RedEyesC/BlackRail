using System;
using System.Collections.Generic;

namespace GameFramework.Runtime
{
    internal class ModuleCenter : GameModule
    {

        private Dictionary<string, BaseCtrl> _ctrlMap = new Dictionary<string, BaseCtrl>();

        private List<Type> _ctrlList = new List<Type>
        {
            typeof(LoginCtrl),
            typeof(SceneCtrl),
            typeof(InputCtrl),
        };


        public override void Destroy()
        {
            _ctrlMap.Clear();
        }

        public override void Start()
        {
            
            foreach ( Type ctrl in _ctrlList)
            {
         
                BaseCtrl Cls = (BaseCtrl)Activator.CreateInstance(ctrl);      
                _ctrlMap[ctrl.Name] = Cls;

            }

        }

        public T GetModule<T>() where T : BaseCtrl
        {
            Type interfaceType = typeof(T);
            if (_ctrlMap.ContainsKey(interfaceType.Name))
            {
                return _ctrlMap[interfaceType.Name] as T;
            }

            return null;
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {

        }



    }

}
