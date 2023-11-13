
using UnityEngine;

namespace GameFramework.Runtime
{
    internal class SceneCtrl : BaseCtrl
    {
        private int _mapId = 0;
        private int _requestId;


        public Role mainRole;

        private Transform _appRoot;

        public SceneCtrl()
        {
            _appRoot = UnityEngine.GameObject.Find("_appRoot").transform;
        }


        public void ClearScene()
        {
            if (_mapId != 0)
            {
                _mapId = 0;

                string path = Utils.GetMapPath(_mapId);
                _requestId = GlobalCenter.GetModule<AssetManager>().UnLoadSceneAsync(path);

            }
        }

        public void LoadScene(int mapId)
        {
            _mapId = mapId;
            string path = Utils.GetMapPath(mapId);
            _requestId = GlobalCenter.GetModule<AssetManager>().LoadSceneAsync(path, OnLoadResFinish);

        }

        private void OnLoadResFinish(int requestID, bool isSuccess)
        {
            if (isSuccess)
            {
                Debug.Log(string.Format("map id {0} is Loaded", _mapId));
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

        public float GetHeightByRayCast(float x, float y)
        {
            float rst = _appRoot.GetHeightByRaycast(x, y, 1 << LayerMask.NameToLayer("Default"));

            return rst;
        }
    }
}
