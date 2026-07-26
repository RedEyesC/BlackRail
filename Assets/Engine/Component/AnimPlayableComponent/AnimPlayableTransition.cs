using System;
using UnityEngine;

public partial class AnimPlayableComponent
{
    public abstract class Transition
    {
        public float FadeDuration { get; set; }
        public bool Restart { get; set; } = true;
        public virtual object Key => this;

        internal abstract State CreateState(AnimPlayableComponent owner);
        internal virtual void Apply(State state) { }
    }

    public sealed class ClipTransition : Transition
    {
        private readonly AnimationClip clip;

        public ClipTransition(AnimationClip clip)
        {
            this.clip = clip;
        }

        public AnimationClip Clip => clip;
        public override object Key => clip;

        internal override State CreateState(AnimPlayableComponent owner)
        {
            return owner.CreateClipState(clip);
        }
    }

    [Serializable]
    public struct LinearMixerChild
    {
        public AnimationClip clip;
        public float threshold;
        public float speed;

        public LinearMixerChild(AnimationClip clip, float threshold, float speed = 1f)
        {
            this.clip = clip;
            this.threshold = threshold;
            this.speed = speed;
        }
    }

    public sealed class LinearMixerTransition : Transition
    {
        private readonly LinearMixerChild[] children;
        private readonly bool extrapolateSpeed;
        private readonly object key;
        private float defaultParameter;

        public LinearMixerTransition(
            LinearMixerChild[] children,
            float defaultParameter,
            bool extrapolateSpeed = false,
            object key = null)
        {
            this.children = children;
            this.defaultParameter = defaultParameter;
            this.extrapolateSpeed = extrapolateSpeed;
            this.key = key;
        }

        public override object Key => key ?? this;

        public float DefaultParameter
        {
            get => defaultParameter;
            set => defaultParameter = value;
        }

        internal override State CreateState(AnimPlayableComponent owner)
        {
            return owner.CreateLinearMixerState(children, defaultParameter, extrapolateSpeed);
        }

        internal override void Apply(State state)
        {
            if (state is LinearMixerState mixerState)
            {
                mixerState.Parameter = defaultParameter;
            }
        }
    }
}
