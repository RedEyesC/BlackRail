using System.Collections.Generic;
using UnityEngine;

public partial class AnimPlayableComponent
{
    private sealed class FadeGroup
    {
        private struct FadeOutState
        {
            public State state;
            public float startWeight;

            public FadeOutState(State state)
            {
                this.state = state;
                startWeight = state.weight;
            }
        }

        private readonly List<FadeOutState> fadeOutStates = new List<FadeOutState>();
        private AnimPlayableComponent owner;
        private State fadeInState;
        private float fadeInStartWeight;
        private float elapsedTime;
        private float duration;

        public bool IsActive => owner != null;

        public void Start(
            AnimPlayableComponent owner,
            State fadeInState,
            IReadOnlyList<State> states,
            float duration)
        {
            Cancel();

            this.owner = owner;
            this.fadeInState = fadeInState;
            this.duration = Mathf.Max(0.0001f, duration);
            fadeInStartWeight = fadeInState.weight;
            elapsedTime = 0f;

            for (int i = states.Count - 1; i >= 0; i--)
            {
                State state = states[i];
                if (ReferenceEquals(state, fadeInState))
                {
                    continue;
                }

                state.OnEnd = null;
                if (state.weight > 0f)
                {
                    fadeOutStates.Add(new FadeOutState(state));
                }
                else
                {
                    owner.StopOrDestroyWeightless(state);
                }
            }

            Apply(0f);
        }

        public void Update(float deltaTime)
        {
            if (!IsActive)
            {
                return;
            }

            elapsedTime += deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            Apply(progress);

            if (progress >= 1f)
            {
                Finish();
            }
        }

        public void Cancel()
        {
            owner = null;
            fadeInState = null;
            fadeOutStates.Clear();
            fadeInStartWeight = 0f;
            elapsedTime = 0f;
            duration = 0f;
        }

        private void Apply(float progress)
        {
            if (!IsActive)
            {
                return;
            }

            if (fadeInState != null && fadeInState.IsValid)
            {
                owner.SetStateWeight(fadeInState, Mathf.Lerp(fadeInStartWeight, 1f, progress));
            }

            for (int i = fadeOutStates.Count - 1; i >= 0; i--)
            {
                FadeOutState fadeOut = fadeOutStates[i];
                if (fadeOut.state != null && fadeOut.state.IsValid)
                {
                    owner.SetStateWeight(fadeOut.state, Mathf.Lerp(fadeOut.startWeight, 0f, progress));
                }
            }
        }

        private void Finish()
        {
            if (!IsActive)
            {
                return;
            }

            if (fadeInState != null && fadeInState.IsValid)
            {
                owner.SetStateWeight(fadeInState, 1f);
            }

            for (int i = fadeOutStates.Count - 1; i >= 0; i--)
            {
                State state = fadeOutStates[i].state;
                if (state != null && state.IsValid)
                {
                    owner.SetStateWeight(state, 0f);
                    owner.StopOrDestroyWeightless(state);
                }
            }

            Cancel();
        }
    }
}
