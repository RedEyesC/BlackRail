using System;
using GameFramework.Common;
using GameFramework.Scene;
using UnityEngine;

namespace GameLogic
{
    internal sealed partial class MovementController
    {
        private readonly MovementSettings settings = new MovementSettings();
        private readonly MovementAnimationNames animationNames = new MovementAnimationNames();
        private readonly string[] locomotionClips = new string[3];
        private readonly float[] locomotionThresholds = { 0f, 1f, 2f };
        private readonly MovementStateMachine stateMachine;
        private readonly Obj owner;

        private Vector3 desiredMoveDirection;
        private Vector3 facingForward = Vector3.forward;
        private Vector3 velocity;
        private Vector3 velocitySpringVelocity;
        private float facingYaw;
        private float facingYawVelocity;
        private float rawMoveInputAmount;
        private float moveInputAmount;
        private float locomotionValue;
        private float moveSpeed;
        private string currentAnimationName;

        public MovementController(Obj owner)
        {
            this.owner = owner;
            stateMachine = new MovementStateMachine(this);
            ChangeToDefaultState();
        }

        public bool IsMoving => HasMoveInput;
        public string CurrentStateName => stateMachine.CurrentStateName;
        public float LocomotionValue => locomotionValue;
        public Vector3 Velocity => velocity;
        public Vector3 DesiredMoveDirection => desiredMoveDirection;
        public MovementSettings Config => settings;
        public MovementAnimationNames AnimationNames => animationNames;

        private bool HasMoveInput => HasRawMoveInput && desiredMoveDirection != Vector3.zero;
        private bool HasRawMoveInput => rawMoveInputAmount > settings.inputDeadZone;
        private bool WantsRun => rawMoveInputAmount >= settings.runInputThreshold;
        private bool HasLocomotionInput => HasMoveInput;

        public void SetMoveInput(Vector3 worldMoveDirection, float inputAmount)
        {
            rawMoveInputAmount = Mathf.Clamp01(inputAmount);
            moveInputAmount = GetEffectiveInputAmount(rawMoveInputAmount);
            desiredMoveDirection = HasRawMoveInput ? NormalizePlanar(worldMoveDirection) : Vector3.zero;
        }

        public void SetFacingForward(Vector3 forward)
        {
            Vector3 planarForward = NormalizePlanar(forward);
            if (planarForward != Vector3.zero)
            {
                facingForward = planarForward;
                facingYaw = DirectionToYaw(facingForward);
                facingYawVelocity = 0f;
            }
        }

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0f, speed);
        }

        public void Update(float deltaTime)
        {
            stateMachine.Update(deltaTime);
            ApplyCodeDrivenMovement(deltaTime);
        }

        public void RefreshState()
        {
            stateMachine.RefreshCurrentState();
        }

        private void ChangeToDefaultState()
        {
            stateMachine.ChangeState(HasLocomotionInput ? stateMachine.MoveStartState : stateMachine.IdleState);
        }

        private void UpdateLocomotion(float deltaTime, float targetValue)
        {
            float lerp = settings.locomotionDampTime <= 0f
                ? 1f
                : 1f - Mathf.Exp(-deltaTime / settings.locomotionDampTime);
            locomotionValue = Mathf.Lerp(locomotionValue, targetValue, lerp);
        }

        private void PlayLocomotion(float parameter)
        {
            locomotionValue = parameter;
            PlayLinearMixerLocomotion();
        }

        private void PlayLinearMixerLocomotion()
        {
            if (owner == null || string.IsNullOrEmpty(animationNames.locomotion))
            {
                return;
            }

            owner.PlayLinearMixerAnim(
                animationNames.locomotion,
                BuildLocomotionClips(),
                locomotionThresholds,
                locomotionValue);
        }

        private bool PlayMovementAnimation(string animationName, Action onEnd = null)
        {
            if (string.IsNullOrEmpty(animationName))
            {
                return false;
            }

            PlayAnimationName(animationName, true, onEnd);
            return true;
        }

        private void PlayAnimationName(string animationName, bool restart, Action onEnd = null)
        {
            if (owner == null || string.IsNullOrEmpty(animationName))
            {
                return;
            }

            if (!restart && currentAnimationName == animationName)
            {
                return;
            }

            currentAnimationName = animationName;
            owner.PlayAnim(animationName, onEnd);
        }

        private string[] BuildLocomotionClips()
        {
            locomotionClips[0] = animationNames.idle;
            locomotionClips[1] = animationNames.walk;
            locomotionClips[2] = animationNames.run;
            return locomotionClips;
        }

        private float GetTargetLocomotionValue()
        {
            return !HasLocomotionInput ? 0f : WantsRun ? 2f : 1f;
        }

        private float GetTargetMoveSpeed()
        {
            if (!HasLocomotionInput)
            {
                return 0f;
            }

            return GetDirectionalMoveSpeed(desiredMoveDirection) * moveInputAmount;
        }

        private void ApplyCodeDrivenMovement(float deltaTime)
        {
            if (deltaTime <= 0f || owner == null || owner.root == null)
            {
                return;
            }

            UpdateVelocity(deltaTime);
            ApplyRotation(deltaTime);
            ApplyPosition(deltaTime);
        }

        private void UpdateVelocity(float deltaTime)
        {
            Vector3 targetVelocity = desiredMoveDirection * GetTargetMoveSpeed();

            Spring.SimpleSpringDamperExact(
                ref velocity.x,
                ref velocitySpringVelocity.x,
                targetVelocity.x,
                settings.velocityHalflife,
                deltaTime);

            Spring.SimpleSpringDamperExact(
                ref velocity.z,
                ref velocitySpringVelocity.z,
                targetVelocity.z,
                settings.velocityHalflife,
                deltaTime);

            velocity.y = 0f;
            velocitySpringVelocity.y = 0f;
        }

        private void ApplyRotation(float deltaTime)
        {
            if (!settings.rotateToMoveDirection || desiredMoveDirection == Vector3.zero)
            {
                return;
            }

            float targetYaw = DirectionToYaw(desiredMoveDirection);
            targetYaw = facingYaw + Mathf.DeltaAngle(facingYaw, targetYaw);

            Spring.SimpleSpringDamperExact(
                ref facingYaw,
                ref facingYawVelocity,
                targetYaw,
                settings.rotationHalflife,
                deltaTime);

            facingForward = YawToDirection(facingYaw);
            owner.SetDir(facingForward.x, facingForward.z);
        }

        private void ApplyPosition(float deltaTime)
        {
            Vector3 delta = velocity * deltaTime;
            if (delta.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            Vector3 position = owner.root.position + delta;
            position.y = SceneManager.GetHeightByRayCast(position.x, position.z);
            owner.SetPosition(position.x, position.y, position.z);
        }

        private static Vector3 NormalizePlanar(Vector3 vector)
        {
            vector.y = 0f;
            return vector.sqrMagnitude > 0.0001f ? vector.normalized : Vector3.zero;
        }

        private static float DirectionToYaw(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        private static Vector3 YawToDirection(float yaw)
        {
            float radians = yaw * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        }

        private float GetEffectiveInputAmount(float inputAmount)
        {
            if (inputAmount <= settings.inputDeadZone)
            {
                return 0f;
            }

            float normalizedInput = Mathf.InverseLerp(settings.inputDeadZone, 1f, inputAmount);
            return Mathf.Pow(normalizedInput, Mathf.Max(0.01f, settings.inputResponsePower));
        }

        private float GetDirectionalMoveSpeed(Vector3 worldDirection)
        {
            Vector3 forward = NormalizePlanar(facingForward);
            if (forward == Vector3.zero || worldDirection == Vector3.zero)
            {
                return GetForwardSpeed();
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float sideAmount = Mathf.Abs(Vector3.Dot(worldDirection, right));
            float forwardAmount = Mathf.Abs(Vector3.Dot(worldDirection, forward));
            float totalAmount = sideAmount + forwardAmount;
            if (totalAmount <= 0.0001f)
            {
                return GetForwardSpeed();
            }

            return (sideAmount * GetSideSpeed() + forwardAmount * GetForwardSpeed()) / totalAmount;
        }

        private float GetForwardSpeed()
        {
            float speedScale = GetMoveSpeedScale();
            return (WantsRun ? settings.runForwardSpeed : settings.walkForwardSpeed) * speedScale;
        }

        private float GetSideSpeed()
        {
            float speedScale = GetMoveSpeedScale();
            return (WantsRun ? settings.runSideSpeed : settings.walkSideSpeed) * speedScale;
        }

        private float GetMoveSpeedScale()
        {
            if (moveSpeed <= 0f || settings.runForwardSpeed <= 0f)
            {
                return 1f;
            }

            return moveSpeed / settings.runForwardSpeed;
        }

        private bool ShouldPlayTurnBack()
        {
            return !string.IsNullOrEmpty(animationNames.turnBack) &&
                   HasLocomotionInput &&
                   desiredMoveDirection != Vector3.zero &&
                   Vector3.Angle(facingForward, desiredMoveDirection) >= settings.turnBackAngle;
        }
    }
}
