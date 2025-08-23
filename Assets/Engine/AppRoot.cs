
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
using UnityEngine;

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

        }

        void Update()
        {
            GameCenter.Update(Time.time, Time.deltaTime);
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
    }
}

