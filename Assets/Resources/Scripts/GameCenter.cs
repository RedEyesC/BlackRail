
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Runtime
{
    public class GameCenter : Singleton<GameCenter>
    {
        private static readonly LinkedList<GameModule> _GameModules = new LinkedList<GameModule>();




        /// <summary>
        /// 所有游戏框架模块唤醒。
        /// </summary>
        public static void Start()
        {
            CreateModule<ModuleCenter>();

            CreateModule<GameLoopManager>();
 

            foreach (GameModule module in _GameModules)
            {
                module.Start();
            }
        }


        /// <summary>
        /// 所有游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public static void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (GameModule module in _GameModules)
            {
                module.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理所有游戏框架模块。
        /// </summary>
        public static void Destroy()
        {
            for (LinkedListNode<GameModule> current = _GameModules.Last; current != null; current = current.Previous)
            {
                current.Value.Destroy();
            }

            _GameModules.Clear();
        }
        /// <summary>
        /// 获取游戏框架模块。
        /// </summary>
        /// <typeparam name="T">要获取的游戏框架模块类型。</typeparam>
        /// <returns>要获取的游戏框架模块。</returns>
        public static T GetModule<T>() where T : class
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

            return GetModule(moduleType) as T;
        }

        /// <summary>
        /// 获取游戏框架模块。
        /// </summary>
        /// <param name="moduleType">要获取的游戏框架模块类型。</param>
        /// <returns>要获取的游戏框架模块。</returns>
        private static GameModule GetModule(Type moduleType)
        {
            foreach (GameModule module in _GameModules)
            {
                if (module.GetType() == moduleType)
                {
                    return module;
                }
            }

            return null;
        }

        /// <summary>
        /// 创建游戏框架模块。
        /// </summary>
        /// <typeparam name="T">要创建的游戏框架模块类型。</typeparam>
        /// <returns>要创建的游戏框架模块类型。</returns>
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

        /// <summary>
        /// 创建游戏框架模块。
        /// </summary>
        /// <param name="moduleType">要创建的游戏框架模块类型。</param>
        /// <returns>要创建的游戏框架模块。</returns>
        private static GameModule CreateModule(Type moduleType)
        {
            GameModule module = (GameModule)Activator.CreateInstance(moduleType);
            if (module == null)
            {
                Debug.LogErrorFormat("Can not create module '{0}'.", moduleType.FullName);
            }

            LinkedListNode<GameModule> current = _GameModules.First;
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
                _GameModules.AddBefore(current, module);
            }
            else
            {
                _GameModules.AddLast(module);
            }

            return module;
        }
    }
}
