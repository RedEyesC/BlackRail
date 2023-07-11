using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GameFramework.Runtime
{
    public class UIManager : GameModule
    {
        public static Vector2 ResolutionSize = new Vector2(1280.0f, 720.0f);

        private GameObject _UIRoot = null;
        private Transform _UIRootTrans = null;
        private Camera _Camera = null;
        private float _ScaleFactor = 1.0f;
     
        public Camera GetCamera()
        {
            return _Camera;
        }

        public override void Start()
        {
            //创建ui相机
            GameObject camObj = new GameObject("UICamera");
            _Camera = CreateUICamera(camObj);
            camObj.SetParent(GameObject.Find("_AppRoot"), false);

            //创建事件监听组件
            CreateEventSystem();

            //创建ui根节点
            _UIRoot = new GameObject("UIRoot");
            _UIRoot.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = _UIRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _Camera;
            canvas.planeDistance = 1000.0f;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;


            float scaleX = screenWidth / ResolutionSize.x;
            float scaleY = screenHeight / ResolutionSize.y;

            CanvasScaler scaler = _UIRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ResolutionSize;
            if (scaleX > scaleY)
                scaler.matchWidthOrHeight = 1.0f;
            else
                scaler.matchWidthOrHeight = 0.0f;

            _UIRootTrans = _UIRoot.transform;
            _UIRootTrans.position = Vector3.zero;

            _UIRoot.SetParent(GameObject.Find("_AppRoot"), false);


            //float aspectRatio = screenWidth / screenHeight;
            //float designedAspectRatio = ResolutionSize.x / ResolutionSize.y;
            //if (aspectRatio < designedAspectRatio)
            //    _ScaleFactor = 1.0f / (((ResolutionSize.x / screenWidth) * screenHeight) / ResolutionSize.y);
            //else
            //    _ScaleFactor = 1.0f;

            //if (scaleX > scaleY)
            //{
            //    _UISize.y = ResolutionSize.y;
            //    _UISize.x = screenWidth / screenHeight * ResolutionSize.y;
            //}
            //else
            //{
            //    _UISize.x = ResolutionSize.x;
            //    _UISize.y = screenHeight / screenWidth * ResolutionSize.x;
            //}

        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {

        }

        public override void Destroy()
        {

        }

        private void CreateEventSystem()
        {
            GameObject ev = new GameObject("EventSystem");
            ev.AddComponent<EventSystem>();
            ev.AddComponent<StandaloneInputModule>();
            ev.SetParent(GameObject.Find("_AppRoot"), false);
        }


        private Camera CreateUICamera(GameObject obj)
        {
            Camera cam = obj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Color;
            cam.cullingMask = 1 << LayerMask.NameToLayer("UI");
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1010.0f;
            cam.orthographic = true;
            cam.orthographicSize = ResolutionSize.y / 2.0f / 100.0f;
            cam.depth = 200;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.transform.position = new Vector3(0.0f, 0.0f, -10000.0f);
            cam.useOcclusionCulling = false;
            cam.backgroundColor = Color.black;
            
            return cam;
        }


        public GameObject CreateLayout(string assetName)
        {
            GameObject viewObj = GlobalCenter.GetModule<AssetManager>().GetAssetObjWithType<GameObject>(assetName);

            GameObject go = GameObject.Instantiate<GameObject>(viewObj);
            _UIRootTrans.AddChild(go.transform);

            return go;
        }

        public void DestroyLayout(GameObject go)
        {
            UnityEngine.GameObject.Destroy(go);
        }


        public void AddToRoot(Transform t, Transform root)
        {
            t.SetParent(root, false);
            Canvas canvas = t.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.worldCamera = _Camera;
            }
        }

        public void AddToUIRoot(Transform t)
        {
            AddToRoot(t, _UIRootTrans);
        }

   
        public void GetUIScaleFactor(out float factor)
        {
            factor = _ScaleFactor;
        }

        public void SetUICamBackgroundColor(Color c)
        {
            _Camera.backgroundColor = c;
        }
    }
}
