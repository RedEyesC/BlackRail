
using GameFramework.Asset;
using GameFramework.Common;
using UnityEngine;

namespace GameFramework.Logger
{
    internal class LoggerManager : GameModule
    {
        public override void Destroy()
        {
           
        }

        public override void Start()
        {
           
        }

        public override void Update(float nowTime, float elapseSeconds)
        {
            InitInGameDebugConsole();
        }

        public void InitInGameDebugConsole()
        {

            GameObject log = new GameObject("Logger");
            log.SetParent(GameObject.Find("_AppRoot"), false);

            string bundleName = "UI/Console";
            AssetManager.LoadAllAssetAsync(bundleName, (Request request) =>
            {
                GameObject obj = AssetManager.GetAssetObjWithType<GameObject>(bundleName, "IngameDebugConsole", true);
                GameObject go = GameObject.Instantiate<GameObject>(obj);
                go.SetParent(log, false);
            });

        }
    }
}
