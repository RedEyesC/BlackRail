using System;
using UnityEngine;
using UnityEngine.Playables;

public partial class AnimPlayableComponent
{
    public class State
    {
        internal AnimPlayableComponent owner;
        internal AnimPlayableLayer layer;
        internal Playable playable;
        internal UnityEngine.Animations.AnimationMixerPlayable mixer;
        internal object key;
        internal int inputIndex;
        internal float weight;
        internal bool keepAliveWhenWeightless;
        internal bool endTriggered;

        internal State(AnimPlayableComponent owner, Playable playable, AnimationClip clip)
        {
            this.owner = owner;
            this.playable = playable;
            Clip = clip;
        }

        public AnimationClip Clip { get; }
        public virtual float Length => Clip != null ? Clip.length : 0f;
        public bool IsValid => owner != null && owner.Contains(this);
        public bool IsCurrent => IsValid && ReferenceEquals(layer?.CurrentState, this);
        public float EndNormalizedTime { get; set; } = 1f;
        public Action OnEnd { get; set; }

        public float Weight
        {
            get => IsValid ? weight : 0f;
            set
            {
                if (IsValid)
                {
                    owner.CancelFade(this);
                    owner.SetStateWeight(this, value);
                }
            }
        }

        public float Time
        {
            get => IsValid ? (float)playable.GetTime() : 0f;
            set
            {
                if (!IsValid)
                {
                    return;
                }

                float time = Mathf.Max(0f, value);
                playable.SetTime(time);
                OnTimeChanged(time);
                endTriggered = false;
            }
        }

        public float NormalizedTime
        {
            get
            {
                float length = Length;
                return length > 0f ? Mathf.Clamp01(Time / length) : 0f;
            }
            set
            {
                float length = Length;
                if (length > 0f)
                {
                    Time = Mathf.Clamp01(value) * length;
                }
            }
        }

        public void Play(bool restart = true)
        {
            if (restart)
            {
                Time = 0f;
            }

            endTriggered = false;
            SetPlaying(true);
        }

        public void Destroy()
        {
            if (IsValid)
            {
                owner.DestroyState(this);
            }
        }

        internal void SetPlaying(bool playing)
        {
            if (!IsValid)
            {
                return;
            }

            SetPlayablePlaying(playable, playing);
            OnPlayingChanged(playing);
        }

        internal virtual void DestroyOwnedPlayables(PlayableGraph graph) { }
        protected virtual void OnTimeChanged(float time) { }
        protected virtual void OnPlayingChanged(bool playing) { }
    }
}
