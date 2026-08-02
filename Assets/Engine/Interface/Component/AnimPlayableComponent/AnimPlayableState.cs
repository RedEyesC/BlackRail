using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GameFramework.Interface
{
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
            private float playbackSpeed = 1f;
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
            public System.Action OnEnd { get; set; }

            public float PlaybackSpeed
            {
                get => playbackSpeed;
                set
                {
                    playbackSpeed = Mathf.Max(0f, value);
                    ApplyPlayableSpeed();
                }
            }

            public virtual float AveragePlanarSpeed
            {
                get
                {
                    Vector3 averageSpeed = Clip != null ? Clip.averageSpeed : Vector3.zero;
                    averageSpeed.y = 0f;
                    return averageSpeed.magnitude;
                }
            }

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

            protected virtual void ApplyPlayableSpeed()
            {
                if (playable.IsValid())
                {
                    playable.SetSpeed(playbackSpeed);
                }
            }

            internal virtual void DestroyOwnedPlayables(PlayableGraph graph) { }

            protected virtual void OnTimeChanged(float time) { }

            protected virtual void OnPlayingChanged(bool playing) { }
        }

        public sealed class LinearMixerState : State
        {
            private readonly AnimationClipPlayable[] childPlayables;
            private readonly AnimationClip[] childClips;
            private readonly float[] thresholds;
            private readonly bool extrapolateSpeed;
            private float parameter;
            private float extrapolatedSpeed = 1f;

            internal LinearMixerState(
                AnimPlayableComponent owner,
                AnimationMixerPlayable playable,
                LinearMixerChild[] children,
                AnimationClipPlayable[] childPlayables,
                bool extrapolateSpeed
            )
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

            public override float AveragePlanarSpeed => GetAveragePlanarSpeed(parameter);

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

            public float GetAveragePlanarSpeed(float parameter)
            {
                if (ChildCount == 0)
                {
                    return 0f;
                }

                if (ChildCount == 1 || parameter <= thresholds[0])
                {
                    return GetChildAveragePlanarSpeed(0);
                }

                for (int i = 1; i < ChildCount; i++)
                {
                    float previousThreshold = thresholds[i - 1];
                    float nextThreshold = thresholds[i];
                    if (parameter > previousThreshold && parameter <= nextThreshold)
                    {
                        float t = Mathf.InverseLerp(previousThreshold, nextThreshold, parameter);
                        return Mathf.Lerp(GetChildAveragePlanarSpeed(i - 1), GetChildAveragePlanarSpeed(i), t);
                    }
                }

                return GetChildAveragePlanarSpeed(ChildCount - 1);
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

            protected override void ApplyPlayableSpeed()
            {
                if (playable.IsValid())
                {
                    playable.SetSpeed(PlaybackSpeed * extrapolatedSpeed);
                }
            }

            private float GetChildAveragePlanarSpeed(int index)
            {
                if (index < 0 || index >= childClips.Length || childClips[index] == null)
                {
                    return 0f;
                }

                Vector3 averageSpeed = childClips[index].averageSpeed;
                averageSpeed.y = 0f;
                return averageSpeed.magnitude;
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
                float secondWeight
            )
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

                extrapolatedSpeed = 1f;
                float maxThreshold = thresholds[thresholds.Length - 1];
                if (extrapolateSpeed && parameter > maxThreshold && maxThreshold > 0f)
                {
                    extrapolatedSpeed *= parameter / maxThreshold;
                }

                ApplyPlayableSpeed();
            }
        }
    }
}
