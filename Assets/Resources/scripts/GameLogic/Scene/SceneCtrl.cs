
using UnityEngine;

namespace GameFramework.Runtime
{
    internal class SceneCtrl:BaseCtrl
    {
        private int _MapId;
        private int _RequestId;


        public void ClearScene()
        {

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


    }
}
