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

            public void RefreshCurrentState()
            {
                MovementState state = currentState;
                currentState = null;
                ChangeState(state);
            }

            public void Update(float deltaTime)
            {
                currentState?.Update(deltaTime);
            }
        }
    }
}
