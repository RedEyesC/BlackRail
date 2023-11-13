

using UnityEngine;

namespace GameFramework.Runtime
{
    public class CameraManager : GameModule
    {
        private GameObject _objLayer = null;
        private Transform _objLayerTrans = null;
        private Camera _camera = null;
        public static Vector2 resolutionSize = new Vector2(1280.0f, 720.0f);


        private RaycastHit _hitResult;

        public float _distance = 10.0f;
        public float _minDist = 2;
        public float _maxDist = 50;
        public Vector3 _distOffset = new Vector3(0, 0.5f, 0);

        public Vector3 _rotation = new Vector3(0, 0, 0);
        public float _minRotX = -7;
        public float _maxRotX = 70;

        private Transform _target = null;

        private Vector3 _tmpVector = new Vector3();

        private bool _colliderCheckEnable = true;
        public Vector3 _colliderOffset = new Vector3(0, 0.5f, 0);
        public float _sphereCastRadius = 0.2f;
        public float _colliderPointOffset = 0.12f;
        private float _colliderDistt = 0f;
        private float _colliderFadeOutSpeed = 2;


        public override void Destroy()
        {

        }

        public override void Start()
        {
            //创建场景相机
            GameObject camObj = new GameObject("MainCamera");
            _camera = CreateSceneCamera(camObj);
            camObj.SetParent(GameObject.Find("_AppRoot"), false);

            UnityEngine.EventSystems.PhysicsRaycaster ray = camObj.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            ray.eventMask = ~LayerMask.NameToLayer("UI");




            _objLayer = new GameObject("ObjLayer");
            _objLayerTrans = _objLayer.transform;
            _objLayerTrans.position = Vector3.zero;
            _objLayer.SetParent(GameObject.Find("_AppRoot"), false);
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (_target == null)
                return;

            Quaternion q = Quaternion.Euler(_rotation);
            //四元数乘以一个3维的向量，表示向量按四元数方向进行旋转
            Vector3 deltaPos = q * Vector3.forward * _distance;
            var targetPos = _target.position;

            var newPos = targetPos - deltaPos;

            //float dist = 0;
            //if (_ColliderCheckEnable)
            //{
            //    Vector3 targetToCamera = newPos - targetPos;
            //    //射线检查返回ture说明，相机与目标之间存在其他物体遮挡
            //    if (Physics.SphereCast(targetPos,
            //        _sphereCastRadius,
            //        targetToCamera,
            //        out _HitResult,
            //        targetToCamera.magnitude)
            //    )
            //    {
            //        dist = _HitResult.distance - _colliderPointOffset;
            //        newPos = targetPos + targetToCamera.normalized * dist;
            //    }
            //}

            //if (dist == 0)
            //{
            //    // 从被其他物体遮挡到无其他物体遮挡，做一个缓动
            //    if (_colliderDistt != 0)
            //    {
            //        _colliderDistt = Mathf.MoveTowards(_colliderDistt, _distance, _colliderFadeOutSpeed);
            //        deltaPos = q * Vector3.forward * _colliderDistt;
            //        newPos = targetPos - deltaPos;
            //        if (_colliderDistt == _distance)
            //            _colliderDistt = 0;
            //    }
            //}
            //else
            //{
            //    _colliderDistt = dist;
            //}

            _camera.transform.position = newPos;

            //根据与遮挡物体的距离进行偏移
            float t = (_maxDist - _colliderDistt) / (_maxDist - _minDist);
            _tmpVector = Vector3.Lerp(Vector3.zero, _distOffset, t);
            _camera.transform.LookAt(targetPos + _tmpVector);
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


            float scaleX = screenWidth / resolutionSize.x;
            float scaleY = screenHeight / resolutionSize.y;

            float scale = Mathf.Max(scaleX, scaleY);

            return (resolutionSize.y * scale) / screenWidth;

        }


        public void AddToObjRoot(Transform t)
        {
            t.SetParent(_objLayerTrans, false);
        }

        public void DestroyLayout(GameObject go)
        {
            UnityEngine.GameObject.Destroy(go);
        }


        public void SetTarget(Transform tran, float dist, float fov, float rx, float ry, float rz)
        {
            _tmpVector.Set(rx, ry, rz);

            _target = tran;
            _distance = dist;
            _rotation = _tmpVector;

            SetSceneCameraFov(fov);
        }

        public void SetSceneCameraFov(float fov)
        {
            if (_camera)
            {
                this._camera.fieldOfView = fov * GetFieldOfViewScale();
            }
        }

    }
}
