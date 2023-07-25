
using UnityEngine;

namespace GameFramework.Runtime
{
    internal class SceneCtrl : BaseCtrl
    {
        private int _MapId = 0;
        private int _RequestId;


        public Role MainRole;


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
            MainRole = new Role();
            MainRole.SetModelID(1, 1);
            return MainRole;
        }



        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if(MainRole != null)
            {
                MainRole.StateUpdate(elapseSeconds, realElapseSeconds);
            }
          

        }

    }
}
