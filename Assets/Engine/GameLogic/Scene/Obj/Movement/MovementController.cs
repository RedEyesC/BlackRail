using GameFramework.Common;
using GameFramework.Interface;
using UnityEngine;

namespace GameLogic
{
    internal sealed partial class MovementController
    {
        static Vector3 tempVec3 = new Vector3();
        public float inputDeadZone = 0.05f;
        public float inputResponsePower = 1.6f;
        public float runInputThreshold = 0.65f;
        public float walkForwardSpeed = 1.75f;
        public float walkSideSpeed = 1.5f;
        public float runForwardSpeed = 4f;
        public float runSideSpeed = 3f;
        public float acceleration = 28f;
        public float brakingDeceleration = 34f;
        public float directionChangeAccelerationMultiplier = 2f;
        public float locomotionExitSpeed = 0.12f;
        public float rotationSpeed = 1080f;
        public float locomotionDampTime = 0.12f;
        public float turnBackAngle = 135f;
        public float turnBackExitAngle = 55f;
        public float turnBackMaxEnterSpeed = 1.4f;
        public bool rotateToMoveDirection = true;
        public bool exitTurnBackWhenAligned = true;
        public bool syncAnimationPlaybackSpeed = true;
        public float animationVelocityScale = 1f;
        public float minAnimationPlaybackSpeed = 0.25f;
        public float maxAnimationPlaybackSpeed = 1.6f;
        public float animationSpeedEpsilon = 0.05f;
        public bool debugLocomotion = false;
        public bool drawLocomotionDebug = false;
        public float debugLocomotionInterval = 0.5f;

        public string locomotion = "Locomotion";
        public string walk = "Walk";
        public string run = "Run";
        public string turnBack = "TurnBack";
        public string idle = "Idle";

        private AnimPlayableComponent.LinearMixerState _locomotionState;
        private AnimPlayableComponent.State _movementAnimationState;

        private readonly MovementStateMachine stateMachine;
        private readonly Obj owner;

        private Vector3 desiredMoveDirection;
        private Vector3 facingForward = Vector3.forward;
        private Vector3 velocity;
        private float desiredYaw;
        private float facingToDesiredAngle;
        private float velocityYaw;
        private float facingToVelocityAngle;
        private float facingYaw;
        private float facingYawVelocity;
        private float rawMoveInputAmount;
        private float moveInputAmount;
        private float locomotionValue;
        private float moveSpeed;
        private string currentAnimationName;
        private Vector3 previousPosition;
        private float measuredPlanarSpeed;
        private float targetMoveSpeed;
        private float effectiveTargetMoveSpeed;
        private float animatedPlanarSpeed;
        private float animationPlaybackSpeed = 1f;
        private float debugLocomotionTimer;
        private bool isTurnBackActive;
        private bool hasPreviousPosition;

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
        public float MeasuredPlanarSpeed => measuredPlanarSpeed;
        public float TargetMoveSpeed => targetMoveSpeed;
        public float EffectiveTargetMoveSpeed => effectiveTargetMoveSpeed;
        public float FacingToDesiredAngle => facingToDesiredAngle;
        public float FacingToVelocityAngle => facingToVelocityAngle;
        public float AnimatedPlanarSpeed => animatedPlanarSpeed;
        public float AnimationPlaybackSpeed => animationPlaybackSpeed;

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
            RefreshTurnData();
            ApplyCodeDrivenMovement(deltaTime);
            RefreshTurnData();
            stateMachine.Update(deltaTime);
            UpdateAnimationPlaybackSpeed();
            UpdateLocomotionDiagnostics(deltaTime);
        }

        private void ChangeToDefaultState()
        {
            stateMachine.ChangeState(ShouldEnterLocomotion() ? stateMachine.MoveLoopState : stateMachine.IdleState);
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
                UpdateLocomotionPlaybackSpeed();
                return;
            }

            AnimPlayableComponent.LinearMixerTransition linearMixerTransition = InitLocomotionChildren();
            linearMixerTransition.DefaultParameter = parameter;
            _locomotionState = owner.PlayAnim(linearMixerTransition) as AnimPlayableComponent.LinearMixerState;
            UpdateLocomotionPlaybackSpeed();
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
            AnimPlayableComponent.State state = owner.PlayAnim(animationName);
            if (state != null)
            {
                _movementAnimationState = state;
                ApplyAnimationPlaybackSpeed(_movementAnimationState);
            }

            return state;
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
            if (HasLocomotionInput)
            {
                return WantsRun ? 2f : 1f;
            }

            return GetLocomotionValueFromSpeed(measuredPlanarSpeed);
        }

        private float GetTargetMoveSpeed()
        {
            if (!HasLocomotionInput)
            {
                return 0f;
            }

            return GetDirectionalMoveSpeed(desiredMoveDirection) * moveInputAmount;
        }

        private bool ShouldEnterLocomotion()
        {
            return HasLocomotionInput || measuredPlanarSpeed > locomotionExitSpeed;
        }

        private bool ShouldReturnToIdle()
        {
            return !HasLocomotionInput && measuredPlanarSpeed <= locomotionExitSpeed;
        }

        private float GetLocomotionValueFromSpeed(float speed)
        {
            if (speed <= 0f)
            {
                return 0f;
            }

            float speedScale = GetMoveSpeedScale();
            float walkSpeed = Mathf.Max(0.0001f, walkForwardSpeed * speedScale);
            float runSpeed = Mathf.Max(walkSpeed + 0.0001f, runForwardSpeed * speedScale);
            if (speed <= walkSpeed)
            {
                return Mathf.InverseLerp(0f, walkSpeed, speed);
            }

            return Mathf.Lerp(1f, 2f, Mathf.InverseLerp(walkSpeed, runSpeed, speed));
        }

        private void UpdateAnimationPlaybackSpeed()
        {
            if (!syncAnimationPlaybackSpeed)
            {
                animationPlaybackSpeed = 1f;
                SetAnimationPlaybackSpeed(_locomotionState, 1f);
                SetAnimationPlaybackSpeed(_movementAnimationState, 1f);
                return;
            }

            AnimPlayableComponent.State currentState = GetCurrentAnimationState();
            if (currentState == null)
            {
                animationPlaybackSpeed = 1f;
                animatedPlanarSpeed = 0f;
                return;
            }

            ApplyAnimationPlaybackSpeed(currentState);
        }

        private void UpdateLocomotionPlaybackSpeed()
        {
            if (_locomotionState == null || !_locomotionState.IsValid)
            {
                return;
            }

            ApplyAnimationPlaybackSpeed(_locomotionState);
        }

        private AnimPlayableComponent.State GetCurrentAnimationState()
        {
            if (_movementAnimationState != null && _movementAnimationState.IsValid && _movementAnimationState.IsCurrent)
            {
                return _movementAnimationState;
            }

            if (_locomotionState != null && _locomotionState.IsValid && _locomotionState.IsCurrent)
            {
                return _locomotionState;
            }

            return null;
        }

        private void ApplyAnimationPlaybackSpeed(AnimPlayableComponent.State state)
        {
            if (state == null || !state.IsValid)
            {
                return;
            }

            animationPlaybackSpeed = CalculateAnimationPlaybackSpeed(state);
            state.PlaybackSpeed = animationPlaybackSpeed;
        }

        private static void SetAnimationPlaybackSpeed(AnimPlayableComponent.State state, float speed)
        {
            if (state != null && state.IsValid)
            {
                state.PlaybackSpeed = speed;
            }
        }

        private float CalculateAnimationPlaybackSpeed(AnimPlayableComponent.State state)
        {
            if (!syncAnimationPlaybackSpeed || state == null)
            {
                return 1f;
            }

            animatedPlanarSpeed = GetStateAnimatedPlanarSpeed(state);

            float targetSpeed = measuredPlanarSpeed > animationSpeedEpsilon ? measuredPlanarSpeed : targetMoveSpeed;
            if (targetSpeed <= animationSpeedEpsilon || animatedPlanarSpeed <= animationSpeedEpsilon)
            {
                return 1f;
            }

            float minSpeed = Mathf.Min(minAnimationPlaybackSpeed, maxAnimationPlaybackSpeed);
            float maxSpeed = Mathf.Max(minAnimationPlaybackSpeed, maxAnimationPlaybackSpeed);
            return Mathf.Clamp(targetSpeed / animatedPlanarSpeed, minSpeed, maxSpeed);
        }

        private float GetStateAnimatedPlanarSpeed(AnimPlayableComponent.State state)
        {
            float sampledSpeed = state.AveragePlanarSpeed * Mathf.Max(0f, animationVelocityScale);
            if (sampledSpeed > animationSpeedEpsilon)
            {
                return sampledSpeed;
            }

            return GetFallbackAnimatedPlanarSpeed();
        }

        private float GetFallbackAnimatedPlanarSpeed()
        {
            if (locomotionValue <= 0f)
            {
                return 0f;
            }

            if (locomotionValue <= 1f)
            {
                return Mathf.Lerp(0f, walkForwardSpeed, locomotionValue);
            }

            return Mathf.Lerp(walkForwardSpeed, runForwardSpeed, Mathf.Clamp01(locomotionValue - 1f));
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
            targetMoveSpeed = GetTargetMoveSpeed();
            effectiveTargetMoveSpeed = targetMoveSpeed;
            Vector3 targetVelocity = desiredMoveDirection * effectiveTargetMoveSpeed;

            float currentAcceleration = GetVelocityAcceleration(targetVelocity);
            velocity = Vector3.MoveTowards(velocity, targetVelocity, currentAcceleration * deltaTime);

            velocity.y = 0f;
        }

        private float GetVelocityAcceleration(Vector3 targetVelocity)
        {
            Vector3 planarVelocity = velocity;
            planarVelocity.y = 0f;
            targetVelocity.y = 0f;

            if (!HasLocomotionInput || targetVelocity.sqrMagnitude <= 0.0001f)
            {
                return brakingDeceleration;
            }

            if (planarVelocity.sqrMagnitude <= 0.0001f)
            {
                return acceleration;
            }

            float directionDot = Vector3.Dot(planarVelocity.normalized, targetVelocity.normalized);
            if (directionDot < 0.35f)
            {
                return acceleration * Mathf.Max(1f, directionChangeAccelerationMultiplier);
            }

            return targetVelocity.sqrMagnitude >= planarVelocity.sqrMagnitude ? acceleration : brakingDeceleration;
        }

        private void ApplyRotation(float deltaTime)
        {
            if (!rotateToMoveDirection || desiredMoveDirection == Vector3.zero)
            {
                return;
            }

            float targetYaw = desiredYaw;
            targetYaw = facingYaw + Mathf.DeltaAngle(facingYaw, targetYaw);

            float yawDelta = Mathf.DeltaAngle(facingYaw, targetYaw);
            float yawStep = Mathf.Clamp(yawDelta, -rotationSpeed * deltaTime, rotationSpeed * deltaTime);

            facingYaw += yawStep;
            facingYawVelocity = deltaTime > 0f ? yawStep / deltaTime : 0f;

            facingForward = YawToDirection(facingYaw);
            owner.SetDir(facingForward.x, facingForward.z);
            RefreshTurnData();
        }

        private void ApplyPosition(float deltaTime)
        {
            Vector3 position = owner.root.position;
            Vector3 beforePosition = position;
            Vector3 delta = velocity * deltaTime;
            if (delta.sqrMagnitude > 0.000001f)
            {
                position += delta;
            }

            position.y = GetHeightByRayCast(position.x, position.z);
            owner.SetPosition(position.x, position.y, position.z);
            UpdateMeasuredPlanarSpeed(beforePosition, position, deltaTime);
        }

        private void UpdateMeasuredPlanarSpeed(Vector3 beforePosition, Vector3 position, float deltaTime)
        {
            Vector3 planarDelta = hasPreviousPosition ? position - previousPosition : position - beforePosition;
            planarDelta.y = 0f;
            measuredPlanarSpeed = deltaTime > 0f ? planarDelta.magnitude / deltaTime : 0f;
            previousPosition = position;
            hasPreviousPosition = true;
        }

        private void UpdateLocomotionDiagnostics(float deltaTime)
        {
            if (owner == null || owner.root == null)
            {
                return;
            }

            if (drawLocomotionDebug)
            {
                Vector3 position = owner.root.position + Vector3.up * 0.08f;
                Debug.DrawLine(position, position + desiredMoveDirection * Mathf.Max(0.1f, targetMoveSpeed), Color.cyan);
                Debug.DrawLine(position, position + velocity, Color.green);
                Debug.DrawLine(position, position + facingForward * Mathf.Max(0.1f, effectiveTargetMoveSpeed), Color.yellow);
            }

            if (!debugLocomotion)
            {
                return;
            }

            debugLocomotionTimer -= deltaTime;
            if (debugLocomotionTimer > 0f)
            {
                return;
            }

            debugLocomotionTimer = Mathf.Max(0.02f, debugLocomotionInterval);
            Debug.Log(
                $"[Locomotion] state={CurrentStateName} input={rawMoveInputAmount:F2}/{moveInputAmount:F2} target={targetMoveSpeed:F2} actual={measuredPlanarSpeed:F2} anim={animatedPlanarSpeed:F2} playRate={animationPlaybackSpeed:F2} blend={locomotionValue:F2} turn={facingToDesiredAngle:F1}"
            );
        }

        private void RefreshTurnData()
        {
            if (desiredMoveDirection != Vector3.zero)
            {
                desiredYaw = DirectionToYaw(desiredMoveDirection);
                facingToDesiredAngle = Mathf.Abs(Mathf.DeltaAngle(facingYaw, desiredYaw));
            }
            else
            {
                facingToDesiredAngle = 0f;
            }

            Vector3 planarVelocity = velocity;
            planarVelocity.y = 0f;
            if (planarVelocity.sqrMagnitude > 0.0001f)
            {
                velocityYaw = DirectionToYaw(planarVelocity);
                facingToVelocityAngle = Mathf.Abs(Mathf.DeltaAngle(facingYaw, velocityYaw));
            }
            else
            {
                facingToVelocityAngle = 0f;
            }
        }

        private void SetTurnBackActive(bool active)
        {
            isTurnBackActive = active;
        }

        private bool ShouldExitTurnBack()
        {
            return exitTurnBackWhenAligned && (!HasLocomotionInput || facingToDesiredAngle <= turnBackExitAngle);
        }

        public static float GetHeightByRayCast(float x, float z)
        {
            int layerMask = 1 << LayerMask.NameToLayer("Default");

            tempVec3.Set(x, 1000, z);
            Ray ray = new Ray(tempVec3, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 1500f, layerMask))
            {
                return hit.point.y;
            }
            return -9999f;
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
                && Mathf.Max(measuredPlanarSpeed, targetMoveSpeed) <= turnBackMaxEnterSpeed
                && facingToDesiredAngle >= turnBackAngle;
        }
    }
}
