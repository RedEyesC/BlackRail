using GameFramework.Asset;
using GameFramework.Scene;

namespace GameFramework.Moudule
{
    internal class SceneCtrl : BaseModule
    {
        private static int _mapId = 0;
        private static SceneRequest _request;


        public static Role mainRole;


        public SceneCtrl()
        {
           
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

        public static Role CreateMainRole()
        {
            mainRole = new Role();
            mainRole.SetModelID(1, 1001);
            return mainRole;
        }


        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (mainRole != null)
            {
                mainRole.StateUpdate(elapseSeconds, realElapseSeconds);
            }

        }


    }
}
