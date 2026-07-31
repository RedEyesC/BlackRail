using GameFramework.Common;
using GameFramework.Interface;
using GameFramework.Scene;
using UnityEngine;

namespace GameLogic
{
    internal sealed partial class MovementController
    {
        public float inputDeadZone = 0.05f;
        public float inputResponsePower = 1.6f;
        public float runInputThreshold = 0.65f;
        public float walkForwardSpeed = 1.75f;
        public float walkSideSpeed = 1.5f;
        public float runForwardSpeed = 4f;
        public float runSideSpeed = 3f;
        public float velocityHalflife = 0.27f;
        public float rotationHalflife = 0.27f;
        public float locomotionDampTime = 0.12f;
        public float stateFadeTime = 0.12f;
        public float turnBackAngle = 135f;
        public bool rotateToMoveDirection = true;

        public string locomotion = "Locomotion";
        public string walkStart = "Walk_Start";
        public string runStart = "Run_Start";
        public string walk = "Walk";
        public string run = "Run";
        public string walkEnd = "Walk_End";
        public string runEnd = "Run_End";
        public string turnBack = "TurnBack";
        public string idle = "Idle";

        private AnimPlayableComponent.LinearMixerState _locomotionState;

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

        private bool HasMoveInput => HasRawMoveInput && desiredMoveDirection != Vector3.zero;
        private bool HasRawMoveInput => rawMoveInputAmount > inputDeadZone;
        private bool WantsRun => rawMoveInputAmount >= runInputThreshold;
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

        private void ChangeToDefaultState()
        {
            stateMachine.ChangeState(HasLocomotionInput ? stateMachine.MoveStartState : stateMachine.IdleState);
        }

        private void UpdateLocomotion(float deltaTime, float targetValue)
        {
            float lerp = locomotionDampTime <= 0f ? 1f : 1f - Mathf.Exp(-deltaTime / locomotionDampTime);
            locomotionValue = Mathf.Lerp(locomotionValue, targetValue, lerp);
        }

        private void PlayLocomotion(float parameter)
        {
            locomotionValue = parameter;
            EnsureLocomotionPlaying(parameter);
            UpdateLocomotionParameter(parameter);
        }

        private void EnsureLocomotionPlaying(float parameter)
        {
            if (_locomotionState != null && _locomotionState.IsValid && _locomotionState.IsCurrent)
            {
                return;
            }

            AnimPlayableComponent.LinearMixerTransition linearMixerTransition = InitLocomotionChildren();
            linearMixerTransition.DefaultParameter = parameter;
            _locomotionState = owner.PlayAnim(linearMixerTransition) as AnimPlayableComponent.LinearMixerState;
        }

        private void UpdateLocomotionParameter(float parameter)
        {
            if (_locomotionState == null || !_locomotionState.IsValid)
            {
                EnsureLocomotionPlaying(parameter);
                return;
            }

            _locomotionState.Parameter = parameter;
        }

        private AnimPlayableComponent.State PlayMovementAnimation(string animationName)
        {
            if (string.IsNullOrEmpty(animationName))
            {
                return null;
            }

            return PlayAnimationName(animationName, true);
        }

        private AnimPlayableComponent.State PlayAnimationName(string animationName, bool restart)
        {
            if (owner == null || string.IsNullOrEmpty(animationName))
            {
                return null;
            }

            if (!restart && currentAnimationName == animationName)
            {
                return null;
            }

            currentAnimationName = animationName;
            return owner.PlayAnim(animationName);
        }

        private AnimPlayableComponent.LinearMixerTransition InitLocomotionChildren()
        {
            AnimPlayableComponent.LinearMixerChild[] locomotionChildren = new AnimPlayableComponent.LinearMixerChild[3];
            locomotionChildren[0] = new AnimPlayableComponent.LinearMixerChild(idle, 0f);
            locomotionChildren[1] = new AnimPlayableComponent.LinearMixerChild(walk, 1f);
            locomotionChildren[2] = new AnimPlayableComponent.LinearMixerChild(run, 2f);

            AnimPlayableComponent.LinearMixerTransition transition = new AnimPlayableComponent.LinearMixerTransition(
                locomotionChildren,
                locomotionValue,
                false,
                locomotion
            );

            return transition;
        }

        private float GetTargetLocomotionValue()
        {
            return !HasLocomotionInput ? 0f
                : WantsRun ? 2f
                : 1f;
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

            Spring.SimpleSpringDamperExact(ref velocity.x, ref velocitySpringVelocity.x, targetVelocity.x, velocityHalflife, deltaTime);

            Spring.SimpleSpringDamperExact(ref velocity.z, ref velocitySpringVelocity.z, targetVelocity.z, velocityHalflife, deltaTime);

            velocity.y = 0f;
            velocitySpringVelocity.y = 0f;
        }

        private void ApplyRotation(float deltaTime)
        {
            if (!rotateToMoveDirection || desiredMoveDirection == Vector3.zero)
            {
                return;
            }

            float targetYaw = DirectionToYaw(desiredMoveDirection);
            targetYaw = facingYaw + Mathf.DeltaAngle(facingYaw, targetYaw);

            Spring.SimpleSpringDamperExact(ref facingYaw, ref facingYawVelocity, targetYaw, rotationHalflife, deltaTime);

            facingForward = YawToDirection(facingYaw);
            owner.SetDir(facingForward.x, facingForward.z);
        }

        private void ApplyPosition(float deltaTime)
        {
            Vector3 position = owner.root.position;
            Vector3 delta = velocity * deltaTime;
            if (delta.sqrMagnitude > 0.000001f)
            {
                position += delta;
            }

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
            if (inputAmount <= inputDeadZone)
            {
                return 0f;
            }

            float normalizedInput = Mathf.InverseLerp(inputDeadZone, 1f, inputAmount);
            return Mathf.Pow(normalizedInput, Mathf.Max(0.01f, inputResponsePower));
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
            return (WantsRun ? runForwardSpeed : walkForwardSpeed) * speedScale;
        }

        private float GetSideSpeed()
        {
            float speedScale = GetMoveSpeedScale();
            return (WantsRun ? runSideSpeed : walkSideSpeed) * speedScale;
        }

        private float GetMoveSpeedScale()
        {
            if (moveSpeed <= 0f || runForwardSpeed <= 0f)
            {
                return 1f;
            }

            return moveSpeed / runForwardSpeed;
        }

        private bool ShouldPlayTurnBack()
        {
            return !string.IsNullOrEmpty(turnBack)
                && HasLocomotionInput
                && desiredMoveDirection != Vector3.zero
                && Vector3.Angle(facingForward, desiredMoveDirection) >= turnBackAngle;
        }
    }
}
