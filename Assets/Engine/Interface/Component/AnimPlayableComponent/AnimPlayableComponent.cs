using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GameFramework.Interface
{
    public partial class AnimPlayableComponent : MonoBehaviour
    {
        private const int DefaultLayerCapacity = 4;

        private Animator animator;

        private PlayableGraph graph;
        private AnimationLayerMixerPlayable layerMixer;
        private AnimPlayableLayer[] layers;
        private int layerCount;

        private readonly Dictionary<object, State> statesByKey = new Dictionary<object, State>();

        private bool hasGraph;

        public bool IsGraphInitialized => hasGraph;

        private void Awake()
        {
            CacheAnimator();
        }

        private void OnDestroy()
        {
            DestroyGraph();
        }

        public void Initialize(Animator targetAnimator = null)
        {
            if (targetAnimator != null)
            {
                animator = targetAnimator;
            }

            RebuildGraph();
        }

        public State Play(AnimationClip clip)
        {
            return Play(clip, 0f);
        }

        public State Play(AnimationClip clip, float fadeDuration, bool restart = false)
        {
            return Play(new ClipTransition(clip) { FadeDuration = fadeDuration, Restart = restart });
        }

        public State Play(Transition transition)
        {
            if (transition == null)
            {
                return null;
            }

            return Play(GetOrCreateState(transition), transition.FadeDuration, transition.Restart);
        }

        public State Play(State state)
        {
            return Play(state, 0f);
        }

        public State Play(State state, float fadeDuration, bool restart = false)
        {
            return Play(0, state, fadeDuration, restart);
        }

        public State Play(int layerIndex, AnimationClip clip, float fadeDuration = 0f, bool restart = true, AvatarMask avatarMask = null)
        {
            AnimPlayableLayer layer = GetLayer(layerIndex);
            State state = CreateClipState(clip, layer);
            if (state == null)
            {
                return null;
            }

            if (avatarMask != null && layerMixer.IsValid())
            {
                layerMixer.SetLayerMaskFromAvatarMask((uint)layerIndex, avatarMask);
            }

            return layer.Play(state, fadeDuration, restart);
        }

        public State Play(int layerIndex, State state, float fadeDuration = 0f, bool restart = false)
        {
            return GetLayer(layerIndex)?.Play(state, fadeDuration, restart);
        }

        public void StopLayer(int layerIndex, State state)
        {
            GetLayer(layerIndex)?.Stop(state);
        }

        public State GetCurrentState(int layerIndex)
        {
            return GetExistingLayer(layerIndex)?.CurrentState;
        }

        public void UpdateGraph(float deltaTime)
        {
            if (!hasGraph || !IsLayerGraphValid() || deltaTime <= 0f)
            {
                return;
            }

            for (int i = 0; i < layerCount; i++)
            {
                layers[i]?.UpdateFade(deltaTime);
            }

            graph.Evaluate(deltaTime);

            for (int i = 0; i < layerCount; i++)
            {
                layers[i]?.UpdateEndEvents();
            }
        }

        internal bool Contains(State state)
        {
            return state != null && ReferenceEquals(state.owner, this) && state.layer != null && state.layer.States.Contains(state);
        }

        private State GetOrCreateState(Transition transition)
        {
            if (!CanCreateState())
            {
                return null;
            }

            object key = transition.Key;
            if (key != null && statesByKey.TryGetValue(key, out State state) && Contains(state))
            {
                transition.Apply(state);
                return state;
            }

            state = transition.CreateState(this);
            if (!Contains(state))
            {
                return null;
            }

            state.key = key;
            state.keepAliveWhenWeightless = key != null;
            if (key != null)
            {
                statesByKey[key] = state;
            }

            transition.Apply(state);
            return state;
        }

        private State CreateClipState(AnimationClip clip)
        {
            return CreateClipState(clip, GetLayer(0));
        }

        private State CreateClipState(AnimationClip clip, AnimPlayableLayer layer)
        {
            if (!CanCreateState() || layer == null || !layer.Mixer.IsValid() || clip == null)
            {
                return null;
            }

            AnimationClipPlayable clipPlayable = CreateClipPlayable(clip);
            return AddState(new State(this, clipPlayable, clip), layer);
        }

        private LinearMixerState CreateLinearMixerState(LinearMixerChild[] children, float defaultParameter, bool extrapolateSpeed)
        {
            if (!CanCreateState())
            {
                return null;
            }

            LinearMixerChild[] validChildren = GetSortedValidChildren(children);
            if (validChildren.Length == 0)
            {
                return null;
            }

            AnimationMixerPlayable childMixer = AnimationMixerPlayable.Create(graph, validChildren.Length);
            AnimationClipPlayable[] childPlayables = CreateChildPlayables(validChildren);
            LinearMixerState state = new LinearMixerState(this, childMixer, validChildren, childPlayables, extrapolateSpeed);

            childMixer.Play();
            AddState(state, GetLayer(0));
            state.ConnectChildren(graph, childMixer);
            state.Parameter = defaultParameter;
            return state;
        }

        private AnimationClipPlayable[] CreateChildPlayables(LinearMixerChild[] children)
        {
            AnimationClipPlayable[] playables = new AnimationClipPlayable[children.Length];
            for (int i = 0; i < children.Length; i++)
            {
                AnimationClipPlayable clipPlayable = CreateClipPlayable(children[i].clip);
                clipPlayable.SetSpeed(Mathf.Approximately(children[i].speed, 0f) ? 1f : children[i].speed);
                playables[i] = clipPlayable;
            }

            return playables;
        }

        private State AddState(State state, AnimPlayableLayer layer)
        {
            state.layer = layer;
            state.mixer = layer.Mixer;
            state.inputIndex = GetFreeInputIndex(layer.Mixer);
            state.weight = 0f;

            graph.Connect(state.playable, 0, layer.Mixer, state.inputIndex);
            layer.Mixer.SetInputWeight(state.inputIndex, 0f);
            layer.States.Add(state);
            return state;
        }

        private AnimationClipPlayable CreateClipPlayable(AnimationClip clip)
        {
            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(true);
            playable.SetTime(0f);
            if (clip.isLooping)
            {
                playable.SetDuration(double.PositiveInfinity);
            }
            else
            {
                playable.SetDuration(clip.length);
            }

            playable.Play();
            return playable;
        }

        private void StopAllExcept(State activeState, List<State> layerStates)
        {
            for (int i = layerStates.Count - 1; i >= 0; i--)
            {
                State state = layerStates[i];
                if (ReferenceEquals(state, activeState))
                {
                    continue;
                }

                state.OnEnd = null;
                SetStateWeight(state, 0f);
                StopOrDestroyWeightless(state);
            }
        }

        private void StopOrDestroyWeightless(State state)
        {
            if (!Contains(state) || state.weight > 0f || (state.layer != null && ReferenceEquals(state.layer.CurrentState, state)))
            {
                return;
            }

            if (state.keepAliveWhenWeightless)
            {
                state.SetPlaying(false);
            }
            else
            {
                DestroyState(state);
            }
        }

        private void DestroyState(State state)
        {
            if (!Contains(state))
            {
                return;
            }

            AnimPlayableLayer layer = state.layer;
            layer.States.Remove(state);
            layer.ClearCurrentState(state);

            if (state.mixer.IsValid())
            {
                state.mixer.DisconnectInput(state.inputIndex);
                state.mixer.SetInputWeight(state.inputIndex, 0f);
            }

            if (state.key != null && statesByKey.TryGetValue(state.key, out State cachedState) && ReferenceEquals(cachedState, state))
            {
                statesByKey.Remove(state.key);
            }

            state.DestroyOwnedPlayables(graph);
            if (state.playable.IsValid())
            {
                graph.DestroyPlayable(state.playable);
            }

            state.owner = null;
            state.layer = null;
            state.mixer = default(AnimationMixerPlayable);
            state.key = null;
            state.OnEnd = null;
        }

        private int GetFreeInputIndex(AnimationMixerPlayable targetMixer)
        {
            int inputCount = targetMixer.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                if (!targetMixer.GetInput(i).IsValid())
                {
                    return i;
                }
            }

            targetMixer.SetInputCount(inputCount + 1);
            return inputCount;
        }

        private bool CanCreateState()
        {
            return hasGraph && layers != null && layerMixer.IsValid();
        }

        private LinearMixerChild[] GetSortedValidChildren(LinearMixerChild[] children)
        {
            if (children == null || children.Length == 0)
            {
                return Array.Empty<LinearMixerChild>();
            }

            List<LinearMixerChild> validChildren = new List<LinearMixerChild>(children.Length);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].clip != null)
                {
                    validChildren.Add(children[i]);
                }
            }

            validChildren.Sort((a, b) => a.threshold.CompareTo(b.threshold));
            return validChildren.ToArray();
        }

        private void SetStateWeight(State state, float weight)
        {
            if (!Contains(state) || !state.mixer.IsValid())
            {
                return;
            }

            state.weight = Mathf.Clamp01(weight);
            state.mixer.SetInputWeight(state.inputIndex, state.weight);
        }

        private void SetLayerWeight(int layerIndex, float weight)
        {
            if (layerMixer.IsValid())
            {
                layerMixer.SetInputWeight(layerIndex, Mathf.Clamp01(weight));
            }
        }

        private void CancelFade(State state)
        {
            state?.layer?.CancelFade();
        }

        private void RebuildGraph()
        {
            DestroyGraph();
            CacheAnimator();

            if (animator == null)
            {
                return;
            }

            graph = PlayableGraph.Create($"{name}_Playable");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            layerMixer = AnimationLayerMixerPlayable.Create(graph, 1);
            layers = new AnimPlayableLayer[DefaultLayerCapacity];

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            output.SetSourcePlayable(layerMixer);

            graph.Play();
            hasGraph = true;
        }

        private void CacheAnimator()
        {
            if (animator != null)
            {
                return;
            }

            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void DestroyGraph()
        {
            ClearLayers();
            statesByKey.Clear();

            if (graph.IsValid())
            {
                graph.Destroy();
            }

            layerMixer = default(AnimationLayerMixerPlayable);
            layers = null;
            layerCount = 0;
            hasGraph = false;
        }

        private AnimPlayableLayer GetLayer(int index)
        {
            SetMinLayerCount(index + 1);
            return layers[index];
        }

        private AnimPlayableLayer GetExistingLayer(int index)
        {
            if (index < 0 || index >= layerCount)
            {
                return null;
            }

            return layers[index];
        }

        private bool IsLayerGraphValid()
        {
            return layerCount > 0 && layers != null && layers[0] != null && layers[0].Mixer.IsValid();
        }

        private void SetMinLayerCount(int min)
        {
            if (min < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(min));
            }

            while (layerCount < min)
            {
                AddLayer();
            }
        }

        private AnimPlayableLayer AddLayer()
        {
            int index = layerCount;
            if (index >= layers.Length)
            {
                Array.Resize(ref layers, layers.Length * 2);
            }

            AnimPlayableLayer layer = new AnimPlayableLayer(this, index);
            layer.SetMixer(AnimationMixerPlayable.Create(graph, 0));

            layerCount = index + 1;
            layerMixer.SetInputCount(layerCount);
            graph.Connect(layer.Mixer, 0, layerMixer, index);
            layerMixer.SetInputWeight(index, index == 0 ? 1f : 0f);

            layers[index] = layer;
            return layer;
        }

        private void ClearLayers()
        {
            if (layers == null)
            {
                return;
            }

            for (int i = 0; i < layerCount; i++)
            {
                DestroyLayerStates(layers[i]);
                layers[i] = null;
            }

            layerCount = 0;
        }

        private void DestroyLayerStates(AnimPlayableLayer layer)
        {
            if (layer == null)
            {
                return;
            }

            for (int i = layer.States.Count - 1; i >= 0; i--)
            {
                DestroyState(layer.States[i]);
            }

            layer.Clear();
        }

        private static void SetPlayablePlaying(Playable playable, bool playing)
        {
            if (playing)
            {
                playable.Play();
            }
            else
            {
                playable.Pause();
            }
        }
    }
}
