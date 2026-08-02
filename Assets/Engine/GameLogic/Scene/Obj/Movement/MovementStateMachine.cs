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
                MoveLoopState = new MoveLoopState(controller);
                TurnBackState = new TurnBackState(controller);
            }

            public IdleState IdleState { get; }
            public MoveLoopState MoveLoopState { get; }
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
                if (controller.ShouldEnterLocomotion())
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.MoveLoopState);
                }
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
                if (controller.ShouldReturnToIdle())
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.IdleState);
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
            private AnimPlayableComponent.State state;

            public TurnBackState(MovementController controller)
                : base(controller) { }

            public override void Enter()
            {
                controller.SetTurnBackActive(true);
                state = controller.PlayMovementAnimation(controller.turnBack);
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
                if (controller.ShouldExitTurnBack())
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.MoveLoopState);
                }
            }

            public override void Exit()
            {
                if (state != null)
                {
                    state.OnEnd = null;
                    state = null;
                }

                controller.SetTurnBackActive(false);
            }

            private void OnTurnBackEnd()
            {
                controller.stateMachine.ChangeState(controller.stateMachine.MoveLoopState);
            }
        }
    }
}
