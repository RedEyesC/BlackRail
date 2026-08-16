using GameFramework.Common;
using GameFramework.Interface;
using UnityEngine;

namespace GameLogic
{
    internal sealed partial class MovementController
    {
        private static Vector3 tempVec3 = new Vector3();

        // Input.
        public float inputDeadZone = 0.05f;
        public float inputResponsePower = 1.6f;
        public float runInputThreshold = 0.65f;

        // Direction-dependent movement speed.
        public float walkForwardSpeed = 1.75f;
        public float walkSideSpeed = 1.5f;
        public float runForwardSpeed = 4f;
        public float runSideSpeed = 3f;

        // Code-driven movement.
        public float acceleration = 28f;
        public float brakingDeceleration = 34f;
        public float directionChangeAccelerationMultiplier = 2f;
        public float rotationSpeed = 1080f;
        public bool rotateToMoveDirection = true;

        // Locomotion and animation transitions.
        public float locomotionExitSpeed = 0.12f;
        public float locomotionDampTime = 0.12f;
        public float stopEnterSpeed = 1.25f;
        public float turnBackAngle = 135f;
        public float turnBackExitAngle = 55f;
        public float turnBackMaxEnterSpeed = 1.4f;

        // Animation speed matching.
        public bool syncAnimationPlaybackSpeed = true;
        public float animationVelocityScale = 1f;
        public float minAnimationPlaybackSpeed = 0.25f;
        public float maxAnimationPlaybackSpeed = 1.6f;
        public float animationSpeedEpsilon = 0.05f;

        // Diagnostics.
        public bool debugLocomotion = false;
        public bool drawLocomotionDebug = false;
        public float debugLocomotionInterval = 0.5f;

        // Animation names.
        public string locomotion = "Locomotion";
        public string walk = "Walk";
        public string run = "Run";
        public string walkStop = "Walk_End";
        public string runStop = "Run_End";
        public string turnBack = "TurnBack";
        public string idle = "Idle";

        private AnimPlayableComponent.LinearMixerState _locomotionState;
        private AnimPlayableComponent.State _movementAnimationState;
        private readonly AnimPlayableComponent.LinearMixerTransition _locomotionTransition;

        private readonly Obj owner;

        private Vector3 velocity;
        private Vector3 previousPosition;
        private Vector3 desiredMoveDirection;
        private Vector3 facingForward = Vector3.forward;
        private float desiredYaw;
        private float facingToDesiredAngle;
        private float facingToVelocityAngle;
        private float facingYaw;
        private float rawMoveInputAmount;
        private float moveInputAmount;
        private float locomotionValue;
        private float moveSpeed;
        private float measuredPlanarSpeed;
        private float targetMoveSpeed;
        private float animatedPlanarSpeed;
        private float animationPlaybackSpeed = 1f;
        private float debugLocomotionTimer;
        private bool hasPreviousPosition;

        private bool isTurnBackActive;
        private bool isStopTransitionActive;

        private bool previousHasLocomotionInput;
        private bool lastWantsRun;
        private bool stopRequested;
        private bool stopWasRun;
        private float stopReleaseSpeed;

        public MovementController(Obj owner)
        {
            this.owner = owner;
            _locomotionTransition = CreateLocomotionTransition();
            RequestMovementAnimations();
            PlayLocomotion(0f);
        }

        public string CurrentStateName => GetCurrentLocomotionDebugStateName();
        public float LocomotionValue => locomotionValue;
        public Vector3 Velocity => velocity;
        public Vector3 DesiredMoveDirection => desiredMoveDirection;
        public float MeasuredPlanarSpeed => measuredPlanarSpeed;
        public float TargetMoveSpeed => targetMoveSpeed;
        public float FacingToDesiredAngle => facingToDesiredAngle;
        public float FacingToVelocityAngle => facingToVelocityAngle;
        public float AnimatedPlanarSpeed => animatedPlanarSpeed;
        public float AnimationPlaybackSpeed => animationPlaybackSpeed;

        private bool HasMoveInput => rawMoveInputAmount > inputDeadZone && desiredMoveDirection != Vector3.zero;
        private bool WantsRun => rawMoveInputAmount >= runInputThreshold;
        private bool HasLocomotionInput => HasMoveInput;

        public void SetMoveInput(Vector3 worldMoveDirection, float inputAmount)
        {
            rawMoveInputAmount = Mathf.Clamp01(inputAmount);
            desiredMoveDirection = rawMoveInputAmount > inputDeadZone
                ? NormalizePlanar(worldMoveDirection)
                : Vector3.zero;
            moveInputAmount = HasLocomotionInput ? GetEffectiveInputAmount(rawMoveInputAmount) : 0f;
        }

        public void SetFacingForward(Vector3 forward)
        {
            Vector3 planarForward = NormalizePlanar(forward);
            if (planarForward != Vector3.zero)
            {
                facingForward = planarForward;
                facingYaw = DirectionToYaw(facingForward);
            }
        }

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0f, speed);
        }

        public void Update(float deltaTime)
        {
            PrepareMovementFrame();
            ApplyCodeDrivenMovement(deltaTime);
            UpdateAnimationFromMovement(deltaTime);
            UpdateLocomotionDiagnostics(deltaTime);
        }

        private void PrepareMovementFrame()
        {
            RefreshTurnData();
            UpdateInputTransitions();
        }

        private void UpdateAnimationFromMovement(float deltaTime)
        {
            RefreshTurnData();
            ClearFinishedMovementTransition();
            UpdateLocomotionFromSnapshot(deltaTime);
            PlayQueuedAnimationTransition();
            UpdateAnimationPlaybackSpeed();
        }

        // Locomotion mixer.
        private void UpdateLocomotion(float deltaTime, float targetValue)
        {
            float lerp = locomotionDampTime <= 0f ? 1f : 1f - Mathf.Exp(-deltaTime / locomotionDampTime);
            locomotionValue = Mathf.Lerp(locomotionValue, targetValue, lerp);
        }

        private void UpdateLocomotionFromSnapshot(float deltaTime)
        {
            float targetValue = GetTargetLocomotionValue();
            UpdateLocomotion(deltaTime, targetValue);

            if (IsMovementTransitionActive())
            {
                UpdateCachedLocomotionParameter();
                if (ShouldInterruptMovementTransition())
                {
                    ReturnToLocomotion();
                }

                return;
            }

            EnsureLocomotionPlaying(locomotionValue);
            UpdateLocomotionParameter(locomotionValue);
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

            _locomotionTransition.DefaultParameter = parameter;
            AnimationPlayResult result = owner.TryPlayAnimation(
                _locomotionTransition,
                out AnimPlayableComponent.State state
            );
            if (result == AnimationPlayResult.Played)
            {
                _locomotionState = state as AnimPlayableComponent.LinearMixerState;
            }

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

        private void UpdateCachedLocomotionParameter()
        {
            if (_locomotionState != null && _locomotionState.IsValid)
            {
                _locomotionState.Parameter = locomotionValue;
            }
        }

        private AnimPlayableComponent.State PlayMovementAnimation(string animationName)
        {
            if (string.IsNullOrEmpty(animationName))
            {
                return null;
            }

            if (owner == null)
            {
                return null;
            }

            AnimationPlayResult result = owner.TryPlayAnimation(animationName, out AnimPlayableComponent.State state);
            if (result == AnimationPlayResult.NotRequested)
            {
                owner.RequestAnimation(animationName);
            }

            if (result == AnimationPlayResult.Played)
            {
                _movementAnimationState = state;
                ApplyAnimationPlaybackSpeed(_movementAnimationState);
            }

            return state;
        }

        private void RequestMovementAnimations()
        {
            owner.RequestAnimation(_locomotionTransition);
            owner.RequestAnimation(walkStop);
            owner.RequestAnimation(runStop);
            owner.RequestAnimation(turnBack);
        }

        private AnimPlayableComponent.LinearMixerTransition CreateLocomotionTransition()
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

            return GetLocomotionValueFromSpeed(GetCurrentPlanarSpeed());
        }

        private float GetTargetMoveSpeed()
        {
            if (!HasLocomotionInput)
            {
                return 0f;
            }

            return GetDirectionalMoveSpeed(desiredMoveDirection) * moveInputAmount;
        }

        private bool ShouldReturnToIdle()
        {
            return !HasLocomotionInput && GetCurrentPlanarSpeed() <= locomotionExitSpeed;
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

        // Playback speed matching.
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

            float currentPlanarSpeed = GetCurrentPlanarSpeed();
            float targetSpeed = currentPlanarSpeed > animationSpeedEpsilon ? currentPlanarSpeed : targetMoveSpeed;
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

        // Input edge detection for transition requests.
        private void UpdateInputTransitions()
        {
            if (HasLocomotionInput)
            {
                ClearQueuedStopTransition();
                lastWantsRun = WantsRun;
                previousHasLocomotionInput = true;
                return;
            }

            if (previousHasLocomotionInput)
            {
                float releaseSpeed = GetCurrentPlanarSpeed();
                stopWasRun = lastWantsRun || releaseSpeed >= GetRunStopSpeedThreshold();
                stopReleaseSpeed = releaseSpeed;
                stopRequested = releaseSpeed >= stopEnterSpeed;
            }
            else if (!stopRequested && GetCurrentPlanarSpeed() <= locomotionExitSpeed)
            {
                ClearQueuedStopTransition();
            }

            previousHasLocomotionInput = false;
        }

        // Code-driven movement.
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
            Vector3 targetVelocity = desiredMoveDirection * targetMoveSpeed;

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

        // Diagnostics.
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
                Debug.DrawLine(position, position + facingForward * Mathf.Max(0.1f, targetMoveSpeed), Color.yellow);
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
                $"[Locomotion] state={CurrentStateName} input={rawMoveInputAmount:F2}/{moveInputAmount:F2} target={targetMoveSpeed:F2} actual={GetCurrentPlanarSpeed():F2} anim={animatedPlanarSpeed:F2} playRate={animationPlaybackSpeed:F2} blend={locomotionValue:F2} turn={facingToDesiredAngle:F1} stop={stopRequested}/{stopReleaseSpeed:F2}"
            );
        }

        // Snapshot data derived from input, velocity and facing.
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
                float velocityYaw = DirectionToYaw(planarVelocity);
                facingToVelocityAngle = Mathf.Abs(Mathf.DeltaAngle(facingYaw, velocityYaw));
            }
            else
            {
                facingToVelocityAngle = 0f;
            }
        }

        private bool ShouldExitTurnBack()
        {
            return !HasLocomotionInput || facingToDesiredAngle <= turnBackExitAngle;
        }

        // World helpers.
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

        // Direction-dependent speed calculation.
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

        // Queued animation transitions.
        private bool ShouldPlayTurnBack()
        {
            return !string.IsNullOrEmpty(turnBack)
                && !IsMovementTransitionActive()
                && HasLocomotionInput
                && desiredMoveDirection != Vector3.zero
                && Mathf.Max(measuredPlanarSpeed, targetMoveSpeed) <= turnBackMaxEnterSpeed
                && facingToDesiredAngle >= turnBackAngle;
        }

        private bool ShouldPlayStop()
        {
            return stopRequested
                && !HasLocomotionInput
                && stopReleaseSpeed >= stopEnterSpeed
                && !string.IsNullOrEmpty(GetStopAnimationName());
        }

        private void PlayQueuedAnimationTransition()
        {
            if (IsMovementTransitionActive())
            {
                return;
            }

            if (ShouldPlayTurnBack())
            {
                PlayTurnBackTransition();
                return;
            }

            PlayQueuedStopTransition();
        }

        private void PlayTurnBackTransition()
        {
            AnimPlayableComponent.State state = PlayMovementAnimation(turnBack);
            if (state == null)
            {
                return;
            }

            isTurnBackActive = true;
            state.EndNormalizedTime = 1f;
            state.OnEnd = OnMovementTransitionEnd;
        }

        private void PlayQueuedStopTransition()
        {
            if (!ShouldPlayStop())
            {
                return;
            }

            AnimPlayableComponent.State state = PlayMovementAnimation(GetStopAnimationName());
            if (state == null)
            {
                return;
            }

            ClearQueuedStopTransition();
            isStopTransitionActive = true;
            state.EndNormalizedTime = 0.7f;
            state.OnEnd = OnMovementTransitionEnd;
        }

        private string GetStopAnimationName()
        {
            return stopWasRun ? runStop : walkStop;
        }

        private float GetRunStopSpeedThreshold()
        {
            return (walkForwardSpeed + runForwardSpeed) * 0.5f * GetMoveSpeedScale();
        }

        private float GetCurrentPlanarSpeed()
        {
            Vector3 planarVelocity = velocity;
            planarVelocity.y = 0f;
            return Mathf.Max(measuredPlanarSpeed, planarVelocity.magnitude);
        }

        private void ClearQueuedStopTransition()
        {
            stopRequested = false;
            stopReleaseSpeed = 0f;
        }

        private bool IsMovementTransitionActive()
        {
            return (isTurnBackActive || isStopTransitionActive)
                && _movementAnimationState != null
                && _movementAnimationState.IsValid
                && _movementAnimationState.IsCurrent;
        }

        private bool ShouldInterruptMovementTransition()
        {
            return (isStopTransitionActive && HasLocomotionInput) || (isTurnBackActive && ShouldExitTurnBack());
        }

        private void ReturnToLocomotion()
        {
            ClearMovementTransition();
            PlayLocomotion(GetTargetLocomotionValue());
        }

        private void ClearMovementTransition()
        {
            if (_movementAnimationState != null)
            {
                _movementAnimationState.OnEnd = null;
            }

            isStopTransitionActive = false;
            isTurnBackActive = false;
        }

        private void ClearFinishedMovementTransition()
        {
            if ((isStopTransitionActive || isTurnBackActive) && !IsMovementTransitionActive())
            {
                ClearMovementTransition();
            }
        }

        private void OnMovementTransitionEnd()
        {
            ReturnToLocomotion();
        }

        private string GetCurrentLocomotionDebugStateName()
        {
            if (isStopTransitionActive)
            {
                return "StopTransition";
            }

            if (isTurnBackActive)
            {
                return "TurnBackTransition";
            }

            return ShouldReturnToIdle() ? "Idle" : "Locomotion";
        }
    }
}
