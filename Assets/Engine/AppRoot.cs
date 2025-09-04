
using GameFramework.AppLoop;
using GameFramework.Asset;
using GameFramework.Common;
using GameFramework.Config;
using GameFramework.Event;
using GameFramework.Input;
using GameFramework.Logger;
using GameFramework.Moudule;
using GameFramework.Scene;
using GameFramework.Timers;
using GameFramework.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using static UnityEngine.LowLevel.PlayerLoopSystem;

namespace GameFramework.Runtime
{
    public class AppRoot : MonoBehaviour
    {

        void Awake()
        {

        }

        void Start()
        {

            InitInterface();

            InitGameCenter();

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#else
            RegisterUpdateFunctions();
#endif

        }


        void OnEarlyUpdate()
        {
            GameCenter.EarlyUpdate();
        }


        void Update()
        {
            GameCenter.Update(Time.time, Time.deltaTime);
        }


        void OnPostLateUpdate()
        {
            GameCenter.PostLateUpdate();
        }

        void OnDestroy()
        {
            GameCenter.Destroy();
        }

        void InitInterface()
        {
            AppInterface.StartCoroutine = StartCoroutine;
            AppInterface.StopCoroutine = StopCoroutine;
            AppInterface.AddComponent = gameObject.AddComponent;
        }

        void InitGameCenter()
        {
            GameCenter.CreateInstance();

            GameCenter.CreateModule<TimerManager>();
            GameCenter.CreateModule<EventManager>();

            GameCenter.CreateModule<UserRecordManger>();

            GameCenter.CreateModule<AssetManager>();

            GameCenter.CreateModule<LoggerManager>();

            GameCenter.CreateModule<ConfigManager>();
            GameCenter.CreateModule<SceneManager>();
            GameCenter.CreateModule<UIManager>();
            GameCenter.CreateModule<InputManager>();
            GameCenter.CreateModule<ModuleManager>();

            GameCenter.CreateModule<AppLoopManager>();

            GameCenter.Start();
        }


        #region updateSystem

#if UNITY_EDITOR
        void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.EnteredPlayMode)
            {
                RegisterUpdateFunctions();
            }
            else if (playModeState == PlayModeStateChange.ExitingPlayMode)
            {
                UnregisterUpdateFunctions();
            }
        }

#endif

        void RegisterUpdateFunctions()
        {
            Listen<EarlyUpdate>(OnEarlyUpdate);
            Listen<PostLateUpdate>(OnPostLateUpdate);
        }

        void UnregisterUpdateFunctions()
        {
            Ignore<PostLateUpdate>(OnPostLateUpdate);
            Ignore<EarlyUpdate>(OnEarlyUpdate);
        }

        public static void Listen<T>(UpdateFunction updateFunction)
        {
            var updateSystems = PlayerLoop.GetCurrentPlayerLoop();
            Listen<T>(ref updateSystems, updateFunction);
            PlayerLoop.SetPlayerLoop(updateSystems);
        }

        public static void Ignore<T>(UpdateFunction updateFunction)
        {
            var updateSystems = PlayerLoop.GetCurrentPlayerLoop();
            Ignore<T>(ref updateSystems, updateFunction);
            PlayerLoop.SetPlayerLoop(updateSystems);
        }

        private static bool Listen<T>(ref PlayerLoopSystem system, UpdateFunction updateFunction)
        {
            if (system.type == typeof(T))
            {
                system.updateDelegate += updateFunction;

                return true;
            }
            else
            {
                if (system.subSystemList != null)
                {
                    for (var i = 0; i < system.subSystemList.Length; i++)
                    {
                        if (Listen<T>(ref system.subSystemList[i], updateFunction))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool Ignore<T>(ref PlayerLoopSystem system, UpdateFunction updateFunction)
        {
            if (system.type == typeof(T))
            {
                system.updateDelegate -= updateFunction;

                return true;
            }
            else
            {
                if (system.subSystemList != null)
                {
                    for (var i = 0; i < system.subSystemList.Length; i++)
                    {
                        if (Ignore<T>(ref system.subSystemList[i], updateFunction))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion 
    }
}

