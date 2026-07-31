using GameFramework.Interface;

namespace GameLogic
{
    internal sealed partial class MovementController
    {
        private abstract class MovementState
        {
            protected readonly MovementController controller;

            protected MovementState(MovementController controller)
            {
                this.controller = controller;
            }

            public virtual void Enter() { }

            public virtual void Exit() { }

            public abstract void Update(float deltaTime);
        }

        private sealed class MovementStateMachine
        {
            private MovementState currentState;

            public MovementStateMachine(MovementController controller)
            {
                IdleState = new IdleState(controller);
                MoveStartState = new MoveStartState(controller);
                MoveLoopState = new MoveLoopState(controller);
                MoveEndState = new MoveEndState(controller);
                TurnBackState = new TurnBackState(controller);
            }

            public IdleState IdleState { get; }
            public MoveStartState MoveStartState { get; }
            public MoveLoopState MoveLoopState { get; }
            public MoveEndState MoveEndState { get; }
            public TurnBackState TurnBackState { get; }
            public string CurrentStateName => currentState?.GetType().Name ?? string.Empty;

            public void ChangeState(MovementState targetState)
            {
                if (targetState == null || currentState == targetState)
                {
                    return;
                }

                currentState?.Exit();
                currentState = targetState;
                currentState.Enter();
            }

            public void Update(float deltaTime)
            {
                currentState?.Update(deltaTime);
            }
        }

        private sealed class IdleState : MovementState
        {
            public IdleState(MovementController controller)
                : base(controller) { }

            public override void Enter()
            {
                controller.PlayLocomotion(0f);
            }

            public override void Update(float deltaTime)
            {
                if (controller.HasLocomotionInput)
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.MoveStartState);
                }
            }
        }

        private sealed class MoveStartState : MovementState
        {
            public MoveStartState(MovementController controller)
                : base(controller) { }

            public override void Enter()
            {
                if (!controller.HasLocomotionInput)
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.IdleState);
                    return;
                }

                string animationName = controller.WantsRun ? controller.runStart : controller.walkStart;
                AnimPlayableComponent.State state = controller.PlayMovementAnimation(animationName);
                if (state == null)
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.MoveLoopState);
                    return;
                }

                state.EndNormalizedTime = 1f;
                state.OnEnd = OnStartEnd;
            }

            public override void Update(float deltaTime)
            {
                controller.UpdateLocomotion(deltaTime, controller.GetTargetLocomotionValue());
            }

            private void OnStartEnd()
            {
                controller.stateMachine.ChangeState(
                    controller.HasLocomotionInput ? controller.stateMachine.MoveLoopState : controller.stateMachine.MoveEndState
                );
            }
        }

        private sealed class MoveLoopState : MovementState
        {
            public MoveLoopState(MovementController controller)
                : base(controller) { }

            public override void Enter()
            {
                controller.PlayLocomotion(controller.GetTargetLocomotionValue());
            }

            public override void Update(float deltaTime)
            {
                if (!controller.HasLocomotionInput)
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.MoveEndState);
                    return;
                }

                if (controller.ShouldPlayTurnBack())
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.TurnBackState);
                    return;
                }

                controller.UpdateLocomotion(deltaTime, controller.GetTargetLocomotionValue());
                controller.UpdateLocomotionParameter(controller.locomotionValue);
            }
        }

        private sealed class TurnBackState : MovementState
        {
            public TurnBackState(MovementController controller)
                : base(controller) { }

            public override void Enter()
            {
                AnimPlayableComponent.State state = controller.PlayMovementAnimation(controller.turnBack);
                if (state == null)
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.MoveLoopState);
                    return;
                }

                state.EndNormalizedTime = 1f;
                state.OnEnd = OnTurnBackEnd;
            }

            public override void Update(float deltaTime)
            {
                controller.UpdateLocomotion(deltaTime, controller.GetTargetLocomotionValue());
            }

            private void OnTurnBackEnd()
            {
                controller.stateMachine.ChangeState(
                    controller.HasLocomotionInput ? controller.stateMachine.MoveLoopState : controller.stateMachine.MoveEndState
                );
            }
        }

        private sealed class MoveEndState : MovementState
        {
            public MoveEndState(MovementController controller)
                : base(controller) { }

            public override void Enter()
            {
                bool fromRun = controller.locomotionValue >= 1.5f;
                string animationName = fromRun ? controller.runEnd : controller.walkEnd;
                AnimPlayableComponent.State state = controller.PlayMovementAnimation(animationName);
                if (state == null)
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.IdleState);
                    return;
                }

                state.EndNormalizedTime = 1f;
                state.OnEnd = OnMoveEnd;
            }

            public override void Update(float deltaTime)
            {
                controller.UpdateLocomotion(deltaTime, 0f);
            }

            private void OnMoveEnd()
            {
                controller.ChangeToDefaultState();
            }
        }
    }
}
