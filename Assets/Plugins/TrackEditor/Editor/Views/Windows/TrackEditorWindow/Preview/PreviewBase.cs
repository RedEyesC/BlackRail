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
            internal readonly IEnumerable<EditorCurveBinding> Bindings;

            internal AnimationSample(AnimationClip animation, float start, float time,
                IEnumerable<EditorCurveBinding> bindings)
            {
                Animation = animation;
                Start = start;
                Time = time;
                Bindings = bindings;
            }
        }

        private static readonly Dictionary<GameObject, PreviewContext> Owners = new Dictionary<GameObject, PreviewContext>();

        private readonly Func<GameObject> resolveTarget;
        private readonly Func<string, AnimationClip> resolveAnimation;
        private AnimationSample? queuedAnimation;
        private PlayableGraph animationGraph;
        private AnimationClipPlayable animationPlayable;
        private Animator animationAnimator;
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

            queuedAnimation = null;
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

        public void QueueAnimation(AnimationClip animation, float start, float time,
            IEnumerable<EditorCurveBinding> bindings)
        {
            queuedAnimation = new AnimationSample(animation, start, time, bindings);
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

        private void SampleQueuedAnimation()
        {
            if (!queuedAnimation.HasValue || !BeginAnimationSampling())
            {
                DestroyAnimationGraph();
                return;
            }

            var sample = queuedAnimation.Value;
            var animator = Target.GetComponent<Animator>();
            var useGraph = animator != null && animator.isActiveAndEnabled && !sample.Animation.legacy;
            if (useGraph)
            {
                PrepareAnimationGraph(animator, sample.Animation);
            }
            else
            {
                DestroyAnimationGraph();
            }

            AddBindings(sample.Bindings);
            var root = Target.transform;
            root.SetPositionAndRotation(OriginPosition, OriginRotation);
            SamplePose(sample.Animation, sample.Start, useGraph);
            var position = root.position;
            var rotation = root.rotation;
            root.SetPositionAndRotation(OriginPosition, OriginRotation);
            SamplePose(sample.Animation, sample.Time, useGraph);
            ApplyRootOffset(root, position, rotation);
        }

        private void ApplyRootOffset(Transform root, Vector3 position, Quaternion rotation)
        {
            var inverse = Quaternion.Inverse(rotation);
            root.SetPositionAndRotation(
                OriginPosition + OriginRotation * (inverse * (root.position - position)),
                OriginRotation * (inverse * root.rotation));
        }

        private void PrepareAnimationGraph(Animator animator, AnimationClip animation)
        {
            if (animationGraph.IsValid() && animationAnimator == animator && animationPlayable.GetAnimationClip() == animation)
            {
                return;
            }

            DestroyAnimationGraph();
            animationAnimator = animator;
            animationGraph = PlayableGraph.Create("TrackEditor Preview");
            animationGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            animationPlayable = AnimationClipPlayable.Create(animationGraph, animation);
            animationPlayable.SetSpeed(0);
            animationPlayable.SetApplyFootIK(false);
            var output = AnimationPlayableOutput.Create(animationGraph, "Animation", animator);
            output.SetSourcePlayable(animationPlayable);
        }

        private void SamplePose(AnimationClip animation, float time, bool useGraph)
        {
            if (useGraph)
            {
                animationPlayable.SetTime(time);
                AnimationMode.SamplePlayableGraph(animationGraph, 0, 0);
            }
            else
            {
                AnimationMode.SampleAnimationClip(Target, animation, time);
            }

            sampledAnimation = true;
        }

        private void DestroyAnimationGraph()
        {
            if (animationGraph.IsValid()) animationGraph.Destroy();
            animationAnimator = null;
        }

        internal void EndFrame()
        {
            try
            {
                SampleQueuedAnimation();
            }
            finally
            {
                queuedAnimation = null;
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
            DestroyAnimationGraph();
            queuedAnimation = null;
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
