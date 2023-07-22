

using UnityEngine;

namespace GameFramework.Runtime
{
    public class CameraManager : GameModule
    {
        private GameObject _ObjLayer = null;
        private Transform _ObjLayerTrans = null;
        private Camera _Camera = null;
        public static Vector2 ResolutionSize = new Vector2(1280.0f, 720.0f);


        private RaycastHit _HitResult;

        public float _Distance = 10.0f;
        public float _MinDist = 2;
        public float _MaxDist = 50;
        public Vector3 _DistOffset = new Vector3(0, 0.5f, 0);

        public Vector3 _Rotation = new Vector3(0, 0, 0);
        public float _MinRotX = -7;
        public float _MaxRotX = 70;

        private Transform _Target = null;

        private Vector3 _TmpVector = new Vector3();

        private bool _ColliderCheckEnable = true;
        public Vector3 _ColliderOffset = new Vector3(0, 0.5f, 0);
        public float _SphereCastRadius = 0.2f;
        public float _ColliderPointOffset = 0.12f;
        private float _ColliderDist = 0f;
        private float _ColliderFadeOutSpeed = 2;


        public override void Destroy()
        {

        }

        public override void Start()
        {
            //创建场景相机
            GameObject camObj = new GameObject("MainCamera");
            _Camera = CreateSceneCamera(camObj);
            camObj.SetParent(GameObject.Find("_AppRoot"), false);

            UnityEngine.EventSystems.PhysicsRaycaster ray = camObj.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            ray.eventMask = ~LayerMask.NameToLayer("UI");




            _ObjLayer = new GameObject("ObjLayer");
            _ObjLayerTrans = _ObjLayer.transform;
            _ObjLayerTrans.position = Vector3.zero;
            _ObjLayer.SetParent(GameObject.Find("_AppRoot"), false);
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (_Target == null)
                return;

            Quaternion q = Quaternion.Euler(_Rotation);
            //四元数乘以一个3维的向量，表示向量按四元数方向进行旋转
            Vector3 deltaPos = q * Vector3.forward * _Distance;
            var targetPos = _Target.position;

            var newPos = targetPos - deltaPos;

            float dist = 0;
            if (_ColliderCheckEnable)
            {
                Vector3 targetToCamera = newPos - targetPos;
                //射线检查返回ture说明，相机与目标之间存在其他物体遮挡
                if (Physics.SphereCast(targetPos,
                    _SphereCastRadius,
                    targetToCamera,
                    out _HitResult,
                    targetToCamera.magnitude)
                )
                {
                    dist = _HitResult.distance - _ColliderPointOffset;
                    newPos = targetPos + targetToCamera.normalized * dist;
                }
            }

            if (dist == 0)
            {
                // 从被其他物体遮挡到无其他物体遮挡，做一个缓动
                if (_ColliderDist != 0)
                {
                    _ColliderDist = Mathf.MoveTowards(_ColliderDist, _Distance, _ColliderFadeOutSpeed);
                    deltaPos = q * Vector3.forward * _ColliderDist;
                    newPos = targetPos - deltaPos;
                    if (_ColliderDist == _Distance)
                        _ColliderDist = 0;
                }
            }
            else
            {
                _ColliderDist = dist;
            }

            _Camera.transform.position = newPos;

            //根据与遮挡物体的距离进行偏移
            float t = (_MaxDist - _ColliderDist) / (_MaxDist - _MinDist);
            _TmpVector = Vector3.Lerp(Vector3.zero, _DistOffset, t);
            _Camera.transform.LookAt(targetPos + _TmpVector);
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


        public void SetTarget(Transform tran, float dist, float fov, float rx, float ry, float rz)
        {
            _TmpVector.Set(rx, ry, rz);

            _Target = tran;
            _Distance = dist;
            _Rotation = _TmpVector;

            SetSceneCameraFov(fov);
        }

        public void SetSceneCameraFov(float fov)
        {
            if (_Camera)
            {
                this._Camera.fieldOfView = fov * GetFieldOfViewScale();
            }
        }

    }
}
