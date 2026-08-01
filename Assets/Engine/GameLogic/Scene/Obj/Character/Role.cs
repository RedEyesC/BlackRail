using GameFramework.Input;
using UnityEngine;

namespace GameLogic
{
    internal class Role : Obj
    {
        private float _moveSpeed = 4f;

        private MovementController _movementController;

        public Role()
            : base(BodyType.Role) { }

        public override void Init(BodyType bodyType)
        {
            base.Init(bodyType);

            SetModelChangeCallback(
                (obj) =>
                {
                    _movementController = new MovementController(this);
                    HandleMoveSpeedChanged();
                }
            );
        }

        private void HandleMoveSpeedChanged()
        {
            _movementController?.SetMoveSpeed(_moveSpeed);
        }

        public override void Update(float nowTime, float elapseSeconds)
        {
            float horizontal = InputManager.GetAxis("Action", "Horizontal");
            float vertical = InputManager.GetAxis("Action", "Vertical");

            if (_movementController != null)
            {
                Vector2 moveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
                Vector3 moveDirection = GetCameraRelativeMoveDirection(moveInput);
                _movementController.SetMoveInput(moveDirection, moveInput.magnitude);

                _movementController.Update(elapseSeconds);
            }

            base.Update(nowTime, elapseSeconds);
        }

        private Vector3 GetCameraRelativeMoveDirection(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            var cameraForwardValue = CameraCtrl.GetMainCameraForward();
            Vector3 cameraForward = new Vector3(cameraForwardValue.x, cameraForwardValue.y, cameraForwardValue.z);
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude <= 0.0001f)
            {
                cameraForward = Vector3.forward;
            }
            else
            {
                cameraForward.Normalize();
            }

            Vector3 cameraLeft = Vector3.Cross(cameraForward, Vector3.up).normalized;
            Vector3 direction = cameraLeft * moveInput.x + cameraForward * moveInput.y;
            direction.y = 0f;

            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        }
    }
}
