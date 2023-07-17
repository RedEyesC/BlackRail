

using UnityEngine;

namespace GameFramework.Runtime
{
    public class CameraManager : GameModule
    {
        private GameObject _ObjLayer = null;
        private Transform _ObjLayerTrans = null;
        private Camera _Camera = null;
        public static Vector2 ResolutionSize = new Vector2(1280.0f, 720.0f);

        public override void Destroy()
        {
           
        }

        public override void Start()
        {
            //创建场景相机
            GameObject camObj = new GameObject("MainCamera");
            _Camera = CreateSceneCamera(camObj);
            camObj.SetParent(GameObject.Find("_AppRoot"), false);

            UnityEngine.EventSystems.PhysicsRaycaster ray =  camObj.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            ray.eventMask = ~LayerMask.NameToLayer("UI");




            _ObjLayer = new GameObject("ObjLayer");
            _ObjLayerTrans = _ObjLayer.transform;
            _ObjLayerTrans.position = Vector3.zero;
            _ObjLayer.SetParent(GameObject.Find("_AppRoot"), false);
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
           
        }

        private Camera CreateSceneCamera(GameObject obj)
        {
            Camera cam = obj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Color;
            cam.cullingMask = 1 << LayerMask.NameToLayer("Default");
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400.0f;
            cam.fieldOfView = GetFieldOfViewScale();
            cam.orthographic = false;
            cam.depth = 100;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.useOcclusionCulling = false;
            cam.backgroundColor = Color.black;

            return cam;
        }

        private float GetFieldOfViewScale()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;


            float scaleX = screenWidth / ResolutionSize.x;
            float scaleY = screenHeight / ResolutionSize.y;
            
            float scale = Mathf.Max(scaleX, scaleY);

            return (ResolutionSize.y * scale) / screenWidth;

        }


        public void AddToObjRoot(Transform t)
        {
            t.SetParent(_ObjLayerTrans, false);
        }

        public void DestroyLayout(GameObject go)
        {
            UnityEngine.GameObject.Destroy(go);
        }


    }
}
