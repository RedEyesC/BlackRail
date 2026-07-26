using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public partial class AnimPlayableComponent
{
    internal bool Contains(State state)
    {
        return state != null && states.Contains(state);
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

    private LinearMixerState CreateLinearMixerState(
        LinearMixerChild[] children,
        float defaultParameter,
        bool extrapolateSpeed)
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
        LinearMixerState state = new LinearMixerState(
            this,
            childMixer,
            validChildren,
            childPlayables,
            extrapolateSpeed);

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
        states.Add(state);
        return state;
    }

    private AnimationClipPlayable CreateClipPlayable(AnimationClip clip)
    {
        AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetApplyFootIK(true);
        playable.SetTime(0f);
        playable.SetDuration(clip.length);
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
        if (!Contains(state) ||
            state.weight > 0f ||
            (state.layer != null && ReferenceEquals(state.layer.CurrentState, state)))
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
        if (state == null || !states.Remove(state))
        {
            return;
        }

        state.layer?.States.Remove(state);
        state.layer?.ClearCurrentState(state);

        if (state.mixer.IsValid())
        {
            state.mixer.DisconnectInput(state.inputIndex);
            state.mixer.SetInputWeight(state.inputIndex, 0f);
        }

        if (state.key != null &&
            statesByKey.TryGetValue(state.key, out State cachedState) &&
            ReferenceEquals(cachedState, state))
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

        ApplyAnimatorSettings();

        graph = PlayableGraph.Create($"{name}_Playable");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        layerMixer = AnimationLayerMixerPlayable.Create(graph, 1);
        layers = new AnimPlayableLayerList(this);

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

    private void ApplyAnimatorSettings()
    {
        if (animator != null)
        {
            animator.applyRootMotion = applyRootMotion;
        }
    }

    private void DestroyGraph()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }

        states.Clear();
        statesByKey.Clear();
        layers?.Clear();

        layerMixer = default(AnimationLayerMixerPlayable);
        layers = null;
        hasGraph = false;
    }

    private AnimPlayableLayer GetLayer(int index)
    {
        return layers?[index];
    }

    private AnimPlayableLayer GetExistingLayer(int index)
    {
        return layers?.GetLayer(index);
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
