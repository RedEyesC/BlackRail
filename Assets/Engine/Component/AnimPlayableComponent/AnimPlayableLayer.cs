using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public partial class AnimPlayableComponent
{
    internal sealed class AnimPlayableLayer
    {
        private readonly AnimPlayableComponent owner;
        private readonly FadeGroup fadeGroup = new FadeGroup();
        private readonly List<State> states = new List<State>();

        public AnimPlayableLayer(AnimPlayableComponent owner, int index)
        {
            this.owner = owner;
            Index = index;
        }

        public int Index { get; }
        public AnimationMixerPlayable Mixer { get; private set; }
        public State CurrentState { get; private set; }
        public List<State> States => states;

        public void SetMixer(AnimationMixerPlayable mixer)
        {
            Mixer = mixer;
        }

        public State Play(State state, float fadeDuration, bool restart = false)
        {
            if (!owner.Contains(state) || !states.Contains(state))
            {
                return null;
            }

            CurrentState = state;
            state.endTriggered = false;
            state.SetPlaying(true);

            if (restart)
            {
                state.Time = 0f;
            }

            owner.SetLayerWeight(Index, 1f);

            if (fadeDuration <= 0f)
            {
                fadeGroup.Cancel();
                owner.StopAllExcept(state, states);
                owner.SetStateWeight(state, 1f);
                return state;
            }

            fadeGroup.Start(owner, state, states, fadeDuration);
            return state;
        }

        public void Stop(State state)
        {
            if (state != null && owner.Contains(state) && states.Contains(state))
            {
                state.OnEnd = null;
                owner.SetStateWeight(state, 0f);
                owner.StopOrDestroyWeightless(state);
            }

            if (ReferenceEquals(CurrentState, state))
            {
                CurrentState = null;
            }

            if (Index > 0 && !HasLiveState())
            {
                owner.SetLayerWeight(Index, 0f);
            }
        }

        public void Update(float deltaTime)
        {
            fadeGroup.Update(deltaTime);

            for (int i = states.Count - 1; i >= 0; i--)
            {
                State state = states[i];
                if (owner.Contains(state))
                {
                    UpdateEndEvent(state);
                }
            }
        }

        public void CancelFade()
        {
            fadeGroup.Cancel();
        }

        public void Clear()
        {
            fadeGroup.Cancel();
            states.Clear();
            CurrentState = null;
            Mixer = default(AnimationMixerPlayable);
        }

        private bool HasLiveState()
        {
            for (int i = states.Count - 1; i >= 0; i--)
            {
                State state = states[i];
                if (state != null && state.IsValid && state.weight > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearCurrentState(State state)
        {
            if (ReferenceEquals(CurrentState, state))
            {
                CurrentState = null;
            }
        }

        private static void UpdateEndEvent(State state)
        {
            if (!(state is LinearMixerState) &&
                !state.endTriggered &&
                state.OnEnd != null &&
                (state.Clip == null || !state.Clip.isLooping) &&
                state.NormalizedTime >= state.EndNormalizedTime)
            {
                state.endTriggered = true;
                System.Action callback = state.OnEnd;
                state.OnEnd = null;
                callback?.Invoke();
            }
        }
    }
}
