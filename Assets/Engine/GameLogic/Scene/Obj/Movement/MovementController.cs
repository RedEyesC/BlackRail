using System;
using GameFramework.Scene;
using UnityEngine;

namespace GameLogic
{
    internal sealed partial class MovementController
    {
        private readonly MovementSettings settings = new MovementSettings();
        private readonly MovementAnimationNames animationNames = new MovementAnimationNames();
        private readonly MovementStateMachine stateMachine;
        private readonly Obj owner;

        private Vector3 desiredMoveDirection = Vector3.forward;
        private Vector3 facingForward = Vector3.forward;
        private Vector3 velocity;
        private float moveInputAmount;
        private bool lockedMovement;
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
        public bool IsLocked => lockedMovement;
        public string CurrentStateName => stateMachine.CurrentStateName;
        public float LocomotionValue => locomotionValue;
        public Vector3 Velocity => velocity;
        public Vector3 DesiredMoveDirection => desiredMoveDirection;
        public MovementSettings Config => settings;
        public MovementAnimationNames AnimationNames => animationNames;

        private MovementSettings Settings => settings;
        private bool HasMoveInput => moveInputAmount > Settings.inputDeadZone;
        private bool WantsRun => moveInputAmount >= Settings.runInputThreshold;
        private bool HasLocomotionInput => HasMoveInput;

        public void SetMoveInput(Vector3 worldMoveDirection, float inputAmount)
        {
            moveInputAmount = Mathf.Clamp01(inputAmount);

            worldMoveDirection.y = 0f;
            if (moveInputAmount <= Settings.inputDeadZone || worldMoveDirection.sqrMagnitude <= 0.0001f)
            {
                desiredMoveDirection = Vector3.zero;
                return;
            }

            desiredMoveDirection = worldMoveDirection.normalized;
        }

        public void SetFacingForward(Vector3 forward)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                facingForward = forward.normalized;
            }
        }

        public void SetLocked(bool locked, Transform target = null)
        {
            lockedMovement = locked;
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
            float lerp = Settings.locomotionDampTime <= 0f
                ? 1f
                : 1f - Mathf.Exp(-deltaTime / Settings.locomotionDampTime);
            locomotionValue = Mathf.Lerp(locomotionValue, targetValue, lerp);
        }

        private void PlayLocomotion(float parameter, bool restart = false)
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
                GetLocomotionAnimationNames(),
                GetLocomotionThresholds(),
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

        private string[] GetLocomotionAnimationNames()
        {
            return new[]
            {
                animationNames.idle,
                animationNames.walk,
                animationNames.run
            };
        }

        private float[] GetLocomotionThresholds()
        {
            return new[] { 0f, 1f, 2f };
        }

        private float GetTargetLocomotionValue()
        {
            if (!HasLocomotionInput)
            {
                return 0f;
            }

            return WantsRun ? 2f : 1f;
        }

        private float GetTargetMoveSpeed()
        {
            if (!HasLocomotionInput)
            {
                return 0f;
            }

            if (moveSpeed > 0f)
            {
                return moveSpeed * (WantsRun ? Settings.runSpeedScale : Settings.walkSpeedScale);
            }

            return WantsRun ? Settings.runSpeed : Settings.walkSpeed;
        }

        private void ApplyCodeDrivenMovement(float deltaTime)
        {
            if (deltaTime <= 0f || owner == null || owner.root == null)
            {
                return;
            }

            Vector3 targetVelocity = HasLocomotionInput
                ? desiredMoveDirection * GetTargetMoveSpeed()
                : Vector3.zero;
            float acceleration = HasLocomotionInput
                ? Settings.maxAcceleration
                : Settings.maxBrakingDeceleration;

            velocity = Vector3.MoveTowards(velocity, targetVelocity, acceleration * deltaTime);
            Vector3 delta = velocity * deltaTime;

            if (Settings.rotateToMoveDirection && desiredMoveDirection.sqrMagnitude > 0.0001f)
            {
                owner.SetDir(desiredMoveDirection.x, desiredMoveDirection.z);
                facingForward = desiredMoveDirection;
            }

            if (delta.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            Vector3 position = ResolveGroundedPosition(owner.root.position + delta);
            owner.SetPosition(position.x, position.y, position.z);
        }

        private Vector3 ResolveGroundedPosition(Vector3 position)
        {
            position.y = SceneManager.GetHeightByRayCast(position.x, position.z);
            return position;
        }

        private bool ShouldPlayTurnBack()
        {
            return !lockedMovement &&
                   !string.IsNullOrEmpty(animationNames.turnBack) &&
                   HasLocomotionInput &&
                   desiredMoveDirection.sqrMagnitude > 0.0001f &&
                   Vector3.Angle(facingForward, desiredMoveDirection) >= Settings.turnBackAngle;
        }
    }
}
