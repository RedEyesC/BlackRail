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
            typeof(GamePoolCtrl),
            typeof(LoginCtrl),
            typeof(SysSettingCtrl),
            typeof(MainCtrl),
            typeof(SceneCtrl),

        };


        public override void Destroy()
        {
            _ctrlMap.Clear();
        }

        public override void Start()
        {

            foreach (Type ctrl in _ctrlList)
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


        public override void EarlyUpdate()
        {
            foreach (var kv in _ctrlMap)
            {
                kv.Value.EarlyUpdate();
            }
        }


        public override void Update(float nowTime, float elapseSeconds)
        {
            foreach (var kv in _ctrlMap)
            {
                kv.Value.Update(nowTime, elapseSeconds);
            }
        }




    }

}
