using System.Collections.Generic;
using GameFramework.Asset;

namespace GameLogic
{
    internal class SceneCtrl : BaseModule
    {
        private static int _mapId = 0;
        private static SceneRequest _request;

        public static Role mainRole;

        private static int _objID = 0;
        private static List<Obj> _objList = new List<Obj>();

        public SceneCtrl() { }

        public void ClearScene()
        {
            if (_mapId != 0)
            {
                _mapId = 0;
                //TODO
                //_requestId = AssetManager.UnLoadSceneAsync(path);
            }
        }

        public static void LoadScene(int mapId)
        {
            _mapId = mapId;
            _request = LoadSceneAsync(mapId);
        }

        public static SceneRequest LoadSceneAsync(int mapId)
        {
            string bundleName = GetSceneBundlePath(mapId);
            string assetName = mapId.ToString();
            return AssetManager.LoadSceneAsync(bundleName, assetName);
        }

        public static string GetSceneBundlePath(int mapId)
        {
            return string.Format("Map/{0}.ab", mapId);
        }

        public static bool IsLoadedScene()
        {
            return _request.isDone;
        }

        public static Role CreateMainRole()
        {
            mainRole = new Role();

            mainRole.SetModelID(ModelType.Body, 1000);

            AddObj(mainRole);

            return mainRole;
        }

        public static Monster CreateMonster(int monsterId)
        {
            Monster monster = GamePoolCtrl.monsterPool.Create();

            monster.InitModel(monsterId);

            AddObj(monster);

            return monster;
        }

        private static void AddObj(Obj obj)
        {
            _objID++;
            _objList.Add(obj);
        }

        public static Role GetMainRole()
        {
            return mainRole;
        }

        public override void EarlyUpdate()
        {
            foreach (var obj in _objList)
            {
                obj.EarlyUpdate();
            }
        }

        public override void Update(float nowTime, float elapseSeconds)
        {
            foreach (var obj in _objList)
            {
                obj.Update(nowTime, elapseSeconds);
            }
        }
    }
}
