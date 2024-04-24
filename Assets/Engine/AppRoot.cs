
using GameFramework.AppLoop;
using GameFramework.Asset;
using GameFramework.Common;
using GameFramework.Event;
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
            GameCenter.Update(Time.deltaTime, Time.unscaledDeltaTime);
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
            GameCenter.CreateModule<AssetManager>();
            GameCenter.CreateModule<SceneManager>();
            GameCenter.CreateModule<UIManager>();
            GameCenter.CreateModule<ModuleManager>();
            GameCenter.CreateModule<AppLoopManager>();

            GameCenter.Start();
        }
    }
}

