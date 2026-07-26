using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public partial class AnimPlayableComponent
{
    public sealed class LinearMixerState : State
    {
        private readonly AnimationClipPlayable[] childPlayables;
        private readonly AnimationClip[] childClips;
        private readonly float[] thresholds;
        private readonly bool extrapolateSpeed;
        private float parameter;

        internal LinearMixerState(
            AnimPlayableComponent owner,
            AnimationMixerPlayable playable,
            LinearMixerChild[] children,
            AnimationClipPlayable[] childPlayables,
            bool extrapolateSpeed)
            : base(owner, playable, children[0].clip)
        {
            this.childPlayables = childPlayables;
            this.extrapolateSpeed = extrapolateSpeed;
            childClips = new AnimationClip[children.Length];
            thresholds = new float[children.Length];

            for (int i = 0; i < children.Length; i++)
            {
                childClips[i] = children[i].clip;
                thresholds[i] = children[i].threshold;
            }
        }

        public override float Length
        {
            get
            {
                float length = 0f;
                for (int i = childClips.Length - 1; i >= 0; i--)
                {
                    if (childClips[i] != null)
                    {
                        length = Mathf.Max(length, childClips[i].length);
                    }
                }

                return length;
            }
        }

        public int ChildCount => childClips.Length;

        public float Parameter
        {
            get => parameter;
            set
            {
                parameter = value;
                UpdateWeights();
                ApplyExtrapolatedSpeed();
            }
        }

        internal void ConnectChildren(PlayableGraph graph, AnimationMixerPlayable mixer)
        {
            for (int i = 0; i < childPlayables.Length; i++)
            {
                graph.Connect(childPlayables[i], 0, mixer, i);
                mixer.SetInputWeight(i, 0f);
            }
        }

        internal override void DestroyOwnedPlayables(PlayableGraph graph)
        {
            for (int i = childPlayables.Length - 1; i >= 0; i--)
            {
                if (childPlayables[i].IsValid())
                {
                    graph.DestroyPlayable(childPlayables[i]);
                }
            }
        }

        protected override void OnTimeChanged(float time)
        {
            for (int i = childPlayables.Length - 1; i >= 0; i--)
            {
                if (childPlayables[i].IsValid())
                {
                    childPlayables[i].SetTime(time);
                }
            }
        }

        protected override void OnPlayingChanged(bool playing)
        {
            for (int i = childPlayables.Length - 1; i >= 0; i--)
            {
                if (childPlayables[i].IsValid())
                {
                    SetPlayablePlaying(childPlayables[i], playing);
                }
            }
        }

        private void UpdateWeights()
        {
            if (!IsValid || ChildCount == 0)
            {
                return;
            }

            AnimationMixerPlayable mixer = (AnimationMixerPlayable)playable;
            if (ChildCount == 1 || parameter <= thresholds[0])
            {
                SetOnlyChildWeight(mixer, 0);
                return;
            }

            for (int i = 1; i < ChildCount; i++)
            {
                float previousThreshold = thresholds[i - 1];
                float nextThreshold = thresholds[i];
                if (parameter > previousThreshold && parameter <= nextThreshold)
                {
                    float t = Mathf.InverseLerp(previousThreshold, nextThreshold, parameter);
                    SetTwoChildWeights(mixer, i - 1, 1f - t, i, t);
                    return;
                }
            }

            SetOnlyChildWeight(mixer, ChildCount - 1);
        }

        private void SetOnlyChildWeight(AnimationMixerPlayable mixer, int activeIndex)
        {
            for (int i = 0; i < ChildCount; i++)
            {
                mixer.SetInputWeight(i, i == activeIndex ? 1f : 0f);
            }
        }

        private void SetTwoChildWeights(
            AnimationMixerPlayable mixer,
            int firstIndex,
            float firstWeight,
            int secondIndex,
            float secondWeight)
        {
            for (int i = 0; i < ChildCount; i++)
            {
                float weight = 0f;
                if (i == firstIndex)
                {
                    weight = firstWeight;
                }
                else if (i == secondIndex)
                {
                    weight = secondWeight;
                }

                mixer.SetInputWeight(i, weight);
            }
        }

        private void ApplyExtrapolatedSpeed()
        {
            if (!IsValid || thresholds.Length == 0)
            {
                return;
            }

            float speed = 1f;
            float maxThreshold = thresholds[thresholds.Length - 1];
            if (extrapolateSpeed && parameter > maxThreshold && maxThreshold > 0f)
            {
                speed *= parameter / maxThreshold;
            }

            playable.SetSpeed(speed);
        }
    }
}
