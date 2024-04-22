using UnityEngine;

namespace GameFramework.Runtime
{
    public class AppLoopStart : StateBase
    {
        public override string GetID()
        {
            return "start";
        }

        public override void StateUpdate(float elapseSeconds, float realElapseSeconds)
        {
            GameCenter.Update(elapseSeconds, realElapseSeconds);
        }

        public override void StateEnter(params object[] paramList)
        {
            InitInGameDebugConsole();

            GameCenter.CreateInstance();
            GameCenter.Start();
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
