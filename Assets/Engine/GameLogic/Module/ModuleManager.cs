using System;
using System.Collections.Generic;

namespace GameLogic
{
    internal class ModuleManager
    {
        private static Dictionary<string, BaseModule> _ctrlMap = new Dictionary<string, BaseModule>();

        private static List<Type> _ctrlList = new List<Type>
        {
            typeof(GamePoolCtrl),
            typeof(LoginCtrl),
            typeof(SysSettingCtrl),
            typeof(MainCtrl),
            typeof(SceneCtrl),
            typeof(CameraCtrl),
        };

        public void Destroy()
        {
            _ctrlMap.Clear();
        }

        public void Start()
        {
            foreach (Type ctrl in _ctrlList)
            {
                BaseModule Cls = (BaseModule)Activator.CreateInstance(ctrl);
                _ctrlMap[ctrl.Name] = Cls;

                Cls.Init();
            }
        }

        public static T GetModule<T>()
            where T : BaseModule
        {
            Type interfaceType = typeof(T);
            if (_ctrlMap.ContainsKey(interfaceType.Name))
            {
                return _ctrlMap[interfaceType.Name] as T;
            }

            return null;
        }

        public void EarlyUpdate()
        {
            foreach (var kv in _ctrlMap)
            {
                kv.Value.EarlyUpdate();
            }
        }

        public void Update(float nowTime, float elapseSeconds)
        {
            foreach (var kv in _ctrlMap)
            {
                kv.Value.Update(nowTime, elapseSeconds);
            }
        }
    }
}
