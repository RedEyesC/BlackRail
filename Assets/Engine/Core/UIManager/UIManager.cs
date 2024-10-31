using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using System;
using System.Collections.Generic;
using GameFramework.Common;
using GameFramework.Scene;
using GameFramework.Asset;

namespace GameFramework.UI
{
    public enum UIZOrder
    {
        UIZOrder_Scene = 1000,
        UIZOrder_Low = 2000,
        UIZOrder_Main_UI = 3000,
        UIZOrder_Common_Below = 4000,
        UIZOrder_Common = 5000,
        UIZOrder_Common_Beyond = 6000,
        UIZOrder_Tips = 7000,
        UIZOrder_Top = 8000,
        UIZOrder_Over = 9000,
    }

    public class UIManager : GameModule
    {
        public static Vector2 resolutionSize = new Vector2(1280.0f, 720.0f);

        private static GameObject _UIRoot = null;
        private static Transform _UIRootTrans = null;
        private static Camera _Camera = null;
        private static float _ScaleFactor = 1.0f;

        private static Dictionary<string, Type> _viewDefines = new Dictionary<string, Type>();
        private static Dictionary<string, BaseView> _viewMap = new Dictionary<string, BaseView>();

        private static Dictionary<int, GameObject> _orderNodeMap = new Dictionary<int, GameObject>();

        public new int priority = 6;
        public override void Start()
        {
            //创建ui相机
            _Camera = CreateUICamera();

            //创建事件监听组件
            CreateEventSystem();

            //初始化ui根节点
            InitUIRoot();

            //初始化order
            InitOrderNode();

        }

        public override void Update(float nowTime, float elapseSeconds)
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


        private Camera CreateUICamera()
        {
            GameObject obj = new GameObject("UICamera");

            obj.SetParent(GameObject.Find("_AppRoot"), false);

            Camera cam = obj.AddComponent<Camera>();

            var cameraData = cam.GetUniversalAdditionalCameraData();
            cameraData.renderType = CameraRenderType.Overlay;

            cam.clearFlags = CameraClearFlags.Depth;
            cam.cullingMask = 1 << LayerMask.NameToLayer("UI");
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1010.0f;
            cam.orthographic = true;
            cam.orthographicSize = resolutionSize.y / 2.0f / 100.0f;
            cam.depth = 200;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.transform.position = new Vector3(0.0f, 0.0f, -10000.0f);
            cam.useOcclusionCulling = false;
            cam.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

            Camera main = SceneManager.GetMainCamera();
            main.GetUniversalAdditionalCameraData().cameraStack.Add(cam);

            return cam;
        }


        public Camera GetCamera()
        {
            return _Camera;
        }

        private void InitUIRoot()
        {

            _UIRoot = new GameObject("UIRoot");
            _UIRoot.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = _UIRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _Camera;
            canvas.planeDistance = 1000.0f;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;


            float scaleX = screenWidth / resolutionSize.x;
            float scaleY = screenHeight / resolutionSize.y;

            CanvasScaler scaler = _UIRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = resolutionSize;
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


        public static GComponent CreateLayout(string bundleName, string assetName)
        {
            GameObject viewObj = AssetManager.GetAssetObjWithType<GameObject>(bundleName, assetName, true);
            GameObject go = GameObject.Instantiate<GameObject>(viewObj);

            _UIRootTrans.AddChild(go.transform);
            GComponent root = new GComponent();
            root.obj = go;
            root.ConstructUI();

            return root;
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


        #region order

        private void InitOrderNode()
        {
            Array orderArray = Enum.GetValues(typeof(UIZOrder));

            foreach (UIZOrder order in orderArray)
            {
                int name = (int)order;
                GameObject obj = new GameObject(name.ToString());
                obj.SetParent(_UIRoot, false);

                _orderNodeMap.Add(name, obj);
            }

        }

        public static void AddViewRoot(BaseView view)
        {
            int uiOrder = view.uiOrder;
            int orderIndex = uiOrder - (uiOrder % 1000);
            GameObject orderNode;
            if (_orderNodeMap.TryGetValue(orderIndex, out orderNode))
            {
                GComponent root = view.GetRoot();
                root.SetParent(orderNode, false);
            }

        }

        #endregion


        #region view

        public static void RegisterView(string viewName, Type viewType)
        {
            if (!_viewDefines.ContainsKey(viewName))
            {
                _viewDefines.Add(viewName, viewType);
            }
        }


        public static void OpenView(string viewName, params object[] paramList)
        {
            BaseView view = GetView(viewName);
            if (view != null)
            {
                view.Open(paramList);
            }
        }

        public static BaseView GetView(string name)
        {
            if (_viewDefines.ContainsKey(name))
            {
                if (!_viewMap.ContainsKey(name))
                {
                    Type type = _viewDefines[name];
                    BaseView view = (BaseView)Activator.CreateInstance(type);
                    _viewMap[name] = view;
                }

                return _viewMap[name];
            }
            else
            {
                return null;
            }


        }

        #endregion
    }
}
