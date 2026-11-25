using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TrackEditor
{
    public abstract class PreviewBase<T> : PreviewBase where T : DirectableAsset
    {
        public T clip => (T)directable;
    }

    public abstract class PreviewBase
    {
        public DirectableAsset directable { get; private set; }
        protected PreviewContext Context { get; private set; }

        internal void Bind(DirectableAsset target, PreviewContext context)
        {
            Context = context;
            SetTarget(target);
        }

        public virtual void SetTarget(DirectableAsset target) => directable = target;

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void ReverseEnter() { }
        public virtual void Reverse() { }
        public virtual void OnSignal(bool reverse) { }
        public abstract void Update(float time, float previousTime);
    }

    public sealed class PreviewContext : IDisposable
    {
        private readonly struct AnimationSample
        {
            internal readonly AnimationClip Animation;
            internal readonly float Start;
            internal readonly float Time;
            internal readonly float Weight;
            internal readonly IEnumerable<EditorCurveBinding> Bindings;

            internal AnimationSample(AnimationClip animation, float start, float time, float weight,
                IEnumerable<EditorCurveBinding> bindings)
            {
                Animation = animation;
                Start = start;
                Time = time;
                Weight = weight;
                Bindings = bindings;
            }
        }

        private static readonly Dictionary<GameObject, PreviewContext> Owners = new Dictionary<GameObject, PreviewContext>();

        private readonly Func<GameObject> resolveTarget;
        private readonly Func<string, AnimationClip> resolveAnimation;
        private readonly Dictionary<Clip, AnimationSample> animationSamples = new Dictionary<Clip, AnimationSample>();
        private readonly List<AnimationSample> trackSamples = new List<AnimationSample>();
        private Clip lastAnimationClip;
        private PlayableGraph blendGraph;
        private AnimationMixerPlayable mixer;
        private Animator blendAnimator;
        private AnimationModeDriver driver;
        private bool sampling;
        private bool sampledAnimation;

        public GameObject Target { get; private set; }
        public float Time { get; private set; }
        public Vector3 OriginPosition { get; private set; }
        public Quaternion OriginRotation { get; private set; }

        public PreviewContext(Func<GameObject> resolveTarget, Func<string, AnimationClip> resolveAnimation)
        {
            this.resolveTarget = resolveTarget;
            this.resolveAnimation = resolveAnimation;
        }

        public AnimationClip ResolveAnimation(string path) =>
            string.IsNullOrEmpty(path) ? null : resolveAnimation(path);

        internal void BindTarget() => Target = resolveTarget();

        internal void BeginFrame(float time)
        {
            Time = time;
            if (Target == null)
            {
                BindTarget();
            }

            animationSamples.Clear();
            lastAnimationClip = null;
            sampledAnimation = false;
            if (driver != null && AnimationMode.InAnimationMode(driver))
            {
                AnimationMode.BeginSampling();
                sampling = true;
            }
        }

        public bool SampleAnimation(AnimationClip clip, float time, IEnumerable<EditorCurveBinding> bindings)
        {
            if (clip == null || !BeginAnimationSampling())
            {
                return false;
            }

            AddBindings(bindings);
            AnimationMode.SampleAnimationClip(Target, clip, time);
            sampledAnimation = true;
            return true;
        }

        public void QueueAnimation(Clip clip, AnimationClip animation, float start, float time,
            IEnumerable<EditorCurveBinding> bindings)
        {
            animationSamples[clip] = new AnimationSample(animation, start, time,
                clip.GetClipWeight(Time - clip.StartTime), bindings);
            lastAnimationClip = clip;
        }

        private bool BeginAnimationSampling()
        {
            if (Target == null || Application.isPlaying
                || (Owners.TryGetValue(Target, out var owner) && owner != this))
            {
                return false;
            }

            if (driver == null)
            {
                OriginPosition = Target.transform.position;
                OriginRotation = Target.transform.rotation;
                driver = ScriptableObject.CreateInstance<AnimationModeDriver>();
                driver.hideFlags = HideFlags.HideAndDontSave;
                AnimationMode.StartAnimationMode(driver);
                if (!AnimationMode.InAnimationMode(driver))
                {
                    UnityEngine.Object.DestroyImmediate(driver);
                    driver = null;
                    return false;
                }

                Owners[Target] = this;
            }

            if (!sampling)
            {
                if (!AnimationMode.InAnimationMode(driver))
                {
                    return false;
                }

                AnimationMode.BeginSampling();
                sampling = true;
            }

            return true;
        }

        private void AddBindings(IEnumerable<EditorCurveBinding> bindings)
        {
            foreach (var binding in bindings)
            {
                AnimationMode.AddEditorCurveBinding(Target, binding);
            }
        }

        private void SampleQueuedAnimations()
        {
            if (lastAnimationClip == null || !BeginAnimationSampling())
            {
                DestroyBlendGraph();
                return;
            }

            var sample = animationSamples[lastAnimationClip];
            trackSamples.Clear();
            var totalWeight = 0f;
            foreach (var clip in lastAnimationClip.Parent.Clips)
            {
                if (animationSamples.TryGetValue(clip, out var candidate) && candidate.Weight > 0)
                {
                    trackSamples.Add(candidate);
                    totalWeight += candidate.Weight;
                }
            }

            if (trackSamples.Count == 0)
            {
                trackSamples.Add(sample);
            }

            var animator = Target.GetComponent<Animator>();
            var useMixer = animator != null && animator.isActiveAndEnabled
                && trackSamples.TrueForAll(candidate => !candidate.Animation.legacy);
            if (useMixer)
            {
                PrepareMixer(animator);
                for (var i = 0; i < trackSamples.Count; i++)
                {
                    AddBindings(trackSamples[i].Bindings);
                    mixer.SetInputWeight(i, trackSamples.Count == 1 ? 1 : trackSamples[i].Weight / totalWeight);
                }
            }
            else
            {
                DestroyBlendGraph();
                foreach (var candidate in trackSamples)
                {
                    if (candidate.Weight > sample.Weight) sample = candidate;
                }
            }

            if (!useMixer) AddBindings(sample.Bindings);
            var root = Target.transform;
            root.SetPositionAndRotation(OriginPosition, OriginRotation);
            SamplePose(sample, useMixer, atStart: true);
            var position = root.position;
            var rotation = root.rotation;
            root.SetPositionAndRotation(OriginPosition, OriginRotation);
            SamplePose(sample, useMixer, atStart: false);
            ApplyRootOffset(root, position, rotation);
        }

        private void ApplyRootOffset(Transform root, Vector3 position, Quaternion rotation)
        {
            var inverse = Quaternion.Inverse(rotation);
            root.SetPositionAndRotation(
                OriginPosition + OriginRotation * (inverse * (root.position - position)),
                OriginRotation * (inverse * root.rotation));
        }

        private void PrepareMixer(Animator animator)
        {
            if (blendGraph.IsValid() && blendAnimator == animator && mixer.GetInputCount() == trackSamples.Count)
            {
                var i = 0;
                while (i < trackSamples.Count
                    && ((AnimationClipPlayable)mixer.GetInput(i)).GetAnimationClip() == trackSamples[i].Animation)
                {
                    i++;
                }

                if (i == trackSamples.Count) return;
            }

            DestroyBlendGraph();
            blendAnimator = animator;
            blendGraph = PlayableGraph.Create("TrackEditor Preview Blend");
            blendGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer = AnimationMixerPlayable.Create(blendGraph, trackSamples.Count);
            for (var i = 0; i < trackSamples.Count; i++)
            {
                var input = AnimationClipPlayable.Create(blendGraph, trackSamples[i].Animation);
                input.SetSpeed(0);
                input.SetApplyFootIK(false);
                blendGraph.Connect(input, 0, mixer, i);
            }

            var output = AnimationPlayableOutput.Create(blendGraph, "Animation", animator);
            output.SetSourcePlayable(mixer);
        }

        private void SamplePose(AnimationSample sample, bool useMixer, bool atStart)
        {
            if (!useMixer)
            {
                AnimationMode.SampleAnimationClip(Target, sample.Animation, atStart ? sample.Start : sample.Time);
                sampledAnimation = true;
                return;
            }

            for (var i = 0; i < trackSamples.Count; i++)
            {
                mixer.GetInput(i).SetTime(atStart ? trackSamples[i].Start : trackSamples[i].Time);
            }

            AnimationMode.SamplePlayableGraph(blendGraph, 0, 0);
            sampledAnimation = true;
        }

        private void DestroyBlendGraph()
        {
            if (blendGraph.IsValid()) blendGraph.Destroy();
            blendAnimator = null;
        }

        internal void EndFrame()
        {
            try
            {
                SampleQueuedAnimations();
            }
            finally
            {
                lastAnimationClip = null;
                animationSamples.Clear();
                trackSamples.Clear();
                EndAnimationSampling();
            }
        }

        private void EndAnimationSampling()
        {
            if (sampling)
            {
                AnimationMode.EndSampling();
                sampling = false;
                if (!sampledAnimation && Target != null)
                {
                    Target.transform.SetPositionAndRotation(OriginPosition, OriginRotation);
                }
            }
        }

        public void Dispose()
        {
            EndAnimationSampling();
            DestroyBlendGraph();
            animationSamples.Clear();
            trackSamples.Clear();
            lastAnimationClip = null;
            if (driver != null)
            {
                if (AnimationMode.InAnimationMode(driver))
                {
                    AnimationMode.StopAnimationMode(driver);
                }

                if (!ReferenceEquals(Target, null))
                {
                    Owners.Remove(Target);
                    if (Target != null)
                    {
                        Target.transform.SetPositionAndRotation(OriginPosition, OriginRotation);
                    }
                }

                UnityEngine.Object.DestroyImmediate(driver);
                driver = null;
            }
        }
    }
}
