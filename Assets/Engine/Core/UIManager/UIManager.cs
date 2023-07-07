using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GameFramework.Runtime
{
    public class UIManager :GameModule
    {
        public static Vector2 ResolutionSize = new Vector2(1280.0f, 720.0f);

        private GameObject mUIRoot = null;
        private Transform mUIRootTrans = null;
        private Camera mCamera = null;
        private float mScaleFactor = 1.0f;
        private Vector2 mUISize;
        

        public Camera GetCamera()
        {
            return mCamera;
        }

        public override void Start()
        {
            //创建ui相机
            GameObject camObj = new GameObject("UICamera");
            mCamera = CreateUICamera(camObj);
            camObj.SetParent(GameObject.Find("_AppRoot"), false);

            mUIRoot = new GameObject("UIRoot");
            mUIRoot.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = mUIRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mCamera;
            canvas.planeDistance = 1000.0f;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;


            float scaleX = screenWidth / ResolutionSize.x;
            float scaleY = screenHeight / ResolutionSize.y;

            CanvasScaler scaler = mUIRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ResolutionSize;
            if (scaleX > scaleY)
                scaler.matchWidthOrHeight = 1.0f;
            else
                scaler.matchWidthOrHeight = 0.0f;
            
            mUIRootTrans = mUIRoot.transform;
            mUIRootTrans.position = Vector3.zero;
            
            mUIRoot.SetParent(GameObject.Find("_AppRoot"), false);

            //GameObject ev = new GameObject("_EventSystem");
            //ev.AddComponent<EventSystem>();
            //ev.AddComponent<StandaloneInputModule>();
            //GameObject.DontDestroyOnLoad(ev);

            //float aspectRatio = screenWidth / screenHeight;
            //float designedAspectRatio = ResolutionSize.x / ResolutionSize.y;
            //if (aspectRatio < designedAspectRatio)
            //    mScaleFactor = 1.0f / (((ResolutionSize.x / screenWidth) * screenHeight) / ResolutionSize.y);
            //else
            //    mScaleFactor = 1.0f;

            //if (scaleX > scaleY)
            //{
            //    mUISize.y = ResolutionSize.y;
            //    mUISize.x = screenWidth / screenHeight * ResolutionSize.y;
            //}
            //else
            //{
            //    mUISize.x = ResolutionSize.x;
            //    mUISize.y = screenHeight / screenWidth * ResolutionSize.x;
            //}

        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {

        }

        public override void Destroy()
        {

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
            cam.depth = 100;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            cam.transform.position = new Vector3(0.0f, 0.0f, -10000.0f);
            cam.useOcclusionCulling = false;
            cam.backgroundColor = Color.white;

            return cam;
        }


        public GameObject CreateLayout(string bundleName, string assetName)
        {
            GameObject panelObject = new GameObject("UIPanel");
            //desc = AssetManager.Instance.GetAssetObjWithType<UIPrefabDesc>(bundleName, assetName);
            //if (desc == null)
            //    return null;

            //desc.Init();

            //Transform t = desc.CreateUIObj();
            //if (t == null)
            //    return null;

            return panelObject;
        }

        public void AddToRoot(Transform t, Transform root)
        {
            t.SetParent(root, false);
            Canvas canvas = t.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.worldCamera = mCamera;
            }
        }

        public void AddToUIRoot(Transform t)
        {
            AddToRoot(t, mUIRootTrans);
        }

        public void GetFullScreenUISize(out float width, out float height)
        {
            width = mUISize.x;
            height = mUISize.y;
        }

        public void GetUIScaleFactor(out float factor)
        {
            factor = mScaleFactor;
        }

        public void SetUICamBackgroundColor(Color c)
        {
            mCamera.backgroundColor = c;
        }
    }
}
