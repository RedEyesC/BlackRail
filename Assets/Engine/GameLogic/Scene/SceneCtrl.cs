using GameFramework.Asset;
using GameFramework.Scene;

namespace GameFramework.Moudule
{
    internal class SceneCtrl : BaseModule
    {
        private static int _mapId = 0;
        private static SceneRequest _requestId;


        public Role mainRole;


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
            _requestId = SceneManager.LoadSceneAsync(mapId);

        }

        private void OnLoadResFinish(int requestID, bool isSuccess)
        {
            if (isSuccess)
            {
                //Debug.Log(string.Format("map id {0} is Loaded", _mapId));
            }

        }


        public Role CreateMainRole()
        {
            mainRole = new Role();
            mainRole.SetModelID(1, 1);
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
