using GameFramework.Common;
using GameLogic;
using System;
using System.Collections.Generic;

namespace GameFramework.Moudule
{
    internal class ModuleManager : GameModule
    {

        private static Dictionary<string, BaseModule> _ctrlMap = new Dictionary<string, BaseModule>();

        public new int priority = 4;

        private static List<Type> _ctrlList = new List<Type>
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
         
                BaseModule Cls = (BaseModule)Activator.CreateInstance(ctrl);      
                _ctrlMap[ctrl.Name] = Cls;

            }

        }

        public static T GetModule<T>() where T : BaseModule
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
