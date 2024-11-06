using GameFramework.Asset;
using GameFramework.Moudule;
using GameFramework.Scene;
using System.Collections.Generic;

namespace GameLogic
{
    public enum BodyType
    {
        Role,
        Monster
    }

    internal class SceneCtrl : BaseModule
    {
        private static int _mapId = 0;
        private static SceneRequest _request;

        public static Role mainRole;

        private static int _objID = 0;
        private static List<Obj> _objList = new List<Obj>();

        public SceneCtrl()
        {
            mainRole = new Role();
        }


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
            _request = SceneManager.LoadSceneAsync(mapId);

        }

        public static bool IsLoadedScene()
        {
            return _request.isDone;
        }


        public static void CreateMainRole()
        {
            mainRole.SetModelID(1, 1001);
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

        public override void Update(float nowTime, float elapseSeconds)
        {
            if (mainRole != null)
            {
                mainRole.StateUpdate(nowTime, elapseSeconds);
            }


            //更新场景里的物体
            foreach (var obj in _objList)
            {
                obj.StateUpdate(nowTime, elapseSeconds);
            }

        }


    }
}
