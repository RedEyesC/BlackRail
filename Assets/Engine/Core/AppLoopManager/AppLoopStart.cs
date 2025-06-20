using GameFramework.Asset;
using GameFramework.Common;
using GameLogic;
using UnityEngine;

namespace GameFramework.AppLoop
{
    public class AppLoopStart : StateBase
    {
        public override string GetID()
        {
            return "Start";
        }

        public override void StateUpdate(float nowTime, float elapseSeconds)
        {
            
        }

        public override void StateEnter(params object[] paramList)
        {
            InitInGameDebugConsole();

            LoginCtrl.OpenLoginView();
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
                go.SetParent(log,false);
            });

        }

    }
}
