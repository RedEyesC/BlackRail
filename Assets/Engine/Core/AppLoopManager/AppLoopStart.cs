using GameFramework.Asset;
using GameFramework.Common;
using GameFramework.Moudule;
using UnityEngine;

namespace GameFramework.AppLoop
{
    public class AppLoopStart : StateBase
    {
        public override string GetID()
        {
            return "Start";
        }

        public override void StateUpdate(float elapseSeconds, float realElapseSeconds)
        {
            
        }

        public override void StateEnter(params object[] paramList)
        {
            InitInGameDebugConsole();

            ModuleManager.GetModule<LoginCtrl>().OpenLoginView();
        }

        public void InitInGameDebugConsole()
        {

            GameObject log = new GameObject("Logger");
            log.SetParent(GameObject.Find("_AppRoot"), false);

            string bundleName = "UI/Console.ab";
            AssetManager.LoadAllAssetAsync(bundleName, (Request request) =>
            {
                GameObject obj = AssetManager.GetAssetObjWithType<GameObject>(bundleName, "IngameDebugConsole", true);
                GameObject go = GameObject.Instantiate<GameObject>(obj);
                go.SetParent(log,false);
            });

        }

    }
}
