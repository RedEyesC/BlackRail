using GameFramework.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    public class GameCenter : Singleton<GameCenter>
    {
        private static readonly LinkedList<GameModule> _gameModules = new LinkedList<GameModule>();

        public static void Start()
        {

            foreach (GameModule module in _gameModules)
            {
                module.Start();
            }
        }

        public static void EarlyUpdate()
        {
            foreach (GameModule module in _gameModules)
            {
                module.EarlyUpdate();
            }
        }

        public static void Update(float nowTime, float elapseSeconds)
        {
            foreach (GameModule module in _gameModules)
            {
                module.Update(nowTime, elapseSeconds);
            }
        }

        public static void PostLateUpdate()
        {
            foreach (GameModule module in _gameModules)
            {
                module.PostLateUpdate();
            }
        }


        public static void Destroy()
        {
            for (LinkedListNode<GameModule> current = _gameModules.Last; current != null; current = current.Previous)
            {
                current.Value.Destroy();
            }

            _gameModules.Clear();
        }


        public static T CreateModule<T>() where T : class
        {
            Type interfaceType = typeof(T);

            if (!interfaceType.FullName.StartsWith("GameFramework.", StringComparison.Ordinal))
            {
                Debug.LogErrorFormat("You must get a Game Framework module, but '{0}' is not.", interfaceType.FullName);
            }

            string moduleName = String.Format("{0}.{1}", interfaceType.Namespace, interfaceType.Name);
            Type moduleType = Type.GetType(moduleName);
            if (moduleType == null)
            {
                Debug.LogErrorFormat("Can not find Game Framework module type '{0}'.", moduleName);
            }

            return CreateModule(moduleType) as T;

        }

        private static GameModule CreateModule(Type moduleType)
        {
            GameModule module = (GameModule)Activator.CreateInstance(moduleType);
            if (module == null)
            {
                Debug.LogErrorFormat("Can not create module '{0}'.", moduleType.FullName);
            }

            LinkedListNode<GameModule> current = _gameModules.First;
            while (current != null)
            {
                if (module.priority > current.Value.priority)
                {
                    break;
                }

                current = current.Next;
            }

            if (current != null)
            {
                _gameModules.AddBefore(current, module);
            }
            else
            {
                _gameModules.AddLast(module);
            }

            return module;
        }
    }
}
