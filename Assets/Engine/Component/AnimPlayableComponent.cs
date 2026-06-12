using System.Collections.Generic;
using TrackEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimPlayableComponent : MonoBehaviour
{
    private Animator _animator;
    private AnimationPlayableOutput _animationOutput;
    public Dictionary<string, AnimationClipPlayable> states = new Dictionary<string, AnimationClipPlayable>();

    private PlayableGraph _graph;

    private void Awake()
    {
        InitializeGraph();
    }

    private void OnDestroy()
    {
        DestroyOutput();

        RestStates();

        DestroyGraph();

        if (_animator != null)
        {
            _animator = null;
        }
    }

    public void Play(string clipName)
    {
        if (states.TryGetValue(clipName, out var state))
        {
            _animationOutput.SetSourcePlayable(state);
            _graph.Play();
        }
    }

    public void Sample(string clipName, float time)
    {
        if (states.TryGetValue(clipName, out var state))
        {
            _animationOutput.SetSourcePlayable(state);
            state.SetSpeed(0);
            state.SetTime(time);
            _graph.Evaluate(0);
        }
    }

    public void SampleFrame(string clipName, int frame)
    {
        if (states.TryGetValue(clipName, out var state))
        {
            var clip = state.GetAnimationClip();
            if (clip == null)
            {
                return;
            }

            Sample(clipName, frame / clip.frameRate);
        }
    }

    public void Play(AnimationClip clip, string clipName)
    {
        var state = AddClip(clip, clipName);
        _animationOutput.SetSourcePlayable(state);
        state.SetSpeed(1);

        _graph.Play();
    }

    public void Stop()
    {
        _graph.Stop();
    }

    public bool TryGetAnimator() => _animator != null || TryGetComponent(out _animator);

    public void InitializeGraph()
    {
        if (_graph.IsValid())
        {
            return;
        }

        TryGetAnimator();

        _graph = PlayableGraph.Create("PlayableGraph");
        _animationOutput = AnimationPlayableOutput.Create(_graph, "AnimationOutput", _animator);
    }

    public AnimationClipPlayable AddClip(AnimationClip clip, string clipName)
    {
        if (!states.TryGetValue(clipName, out var state))
        {
            state = CreateState(clip);
            states[clipName] = state;
        }

        return state;
    }

    public AnimationClipPlayable CreateState(AnimationClip clip)
    {
        return AnimationClipPlayable.Create(_graph, clip);
    }

    public void RestStates()
    {
        // 遍历并销毁每个值
        foreach (var key in new List<string>(states.Keys))
        {
            if (_graph.IsValid())
            {
                _graph.DestroyPlayable(states[key]);
            }

            states.Remove(key);
        }

        states.Clear();
    }

    public bool DestroyOutput()
    {
        if (!_animationOutput.IsOutputValid())
            return false;

        _graph.DestroyOutput(_animationOutput);
        return true;
    }

    public bool DestroyGraph()
    {
        if (!_graph.IsValid())
            return false;

        _graph.Destroy();
        return true;
    }
}
