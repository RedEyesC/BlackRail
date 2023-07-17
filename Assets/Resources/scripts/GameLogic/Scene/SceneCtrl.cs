
using UnityEngine;

namespace GameFramework.Runtime
{
    internal class SceneCtrl:BaseCtrl
    {
        private int _MapId = 0;
        private int _RequestId;


        private Role _Role;

        public void ClearScene()
        {
            if(_MapId!= 0)
            {
                _MapId = 0;

                string path = Utils.GetMapPath(_MapId);
                _RequestId = GlobalCenter.GetModule<AssetManager>().UnLoadSceneAsync(path);

            }
        }

        public void LoadScene(int mapId)
        {
            _MapId = mapId;
            string path = Utils.GetMapPath(mapId);
            _RequestId = GlobalCenter.GetModule<AssetManager>().LoadSceneAsync(path, OnLoadResFinish);

        }

        private void OnLoadResFinish(int requestID, bool isSuccess)
        {
            if (isSuccess)
            {
                Debug.Log(string.Format("map id {0} is Loaded", _MapId));
            }
           
        }


        public Role CreateMainRole()
        {
            Role role = new Role();
            role.SetModelID(1, 1);
            return role;
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {

        }



    }
}
