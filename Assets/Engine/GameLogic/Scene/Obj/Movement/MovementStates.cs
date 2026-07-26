namespace GameLogic
{
    internal sealed partial class MovementController
    {
        private sealed class IdleState : MovementState
        {
            public IdleState(MovementController controller) : base(controller) { }

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
            public MoveStartState(MovementController controller) : base(controller) { }

            public override void Enter()
            {
                if (!controller.HasLocomotionInput)
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.IdleState);
                    return;
                }

                string animationName = controller.WantsRun
                    ? controller.animationNames.runStart
                    : controller.animationNames.walkStart;
                bool started = controller.PlayMovementAnimation(animationName, OnStartEnd);
                if (!started)
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.MoveLoopState);
                }
            }

            public override void Update(float deltaTime)
            {
                controller.UpdateLocomotion(deltaTime, controller.GetTargetLocomotionValue());
            }

            private void OnStartEnd()
            {
                controller.stateMachine.ChangeState(
                    controller.HasLocomotionInput
                        ? controller.stateMachine.MoveLoopState
                        : controller.stateMachine.MoveEndState
                );
            }
        }

        private sealed class MoveLoopState : MovementState
        {
            public MoveLoopState(MovementController controller) : base(controller) { }

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
                controller.PlayLocomotion(controller.locomotionValue);
            }
        }

        private sealed class TurnBackState : MovementState
        {
            public TurnBackState(MovementController controller) : base(controller) { }

            public override void Enter()
            {
                bool started = controller.PlayMovementAnimation(controller.animationNames.turnBack, OnTurnBackEnd);
                if (!started)
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.MoveLoopState);
                }
            }

            public override void Update(float deltaTime)
            {
                controller.UpdateLocomotion(deltaTime, controller.GetTargetLocomotionValue());
            }

            private void OnTurnBackEnd()
            {
                controller.stateMachine.ChangeState(
                    controller.HasLocomotionInput
                        ? controller.stateMachine.MoveLoopState
                        : controller.stateMachine.MoveEndState
                );
            }
        }

        private sealed class MoveEndState : MovementState
        {
            public MoveEndState(MovementController controller) : base(controller) { }

            public override void Enter()
            {
                bool fromRun = controller.locomotionValue >= 1.5f;
                string animationName = fromRun
                    ? controller.animationNames.runEnd
                    : controller.animationNames.walkEnd;
                bool started = controller.PlayMovementAnimation(animationName, OnMoveEnd);
                if (!started)
                {
                    controller.stateMachine.ChangeState(controller.stateMachine.IdleState);
                }
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
