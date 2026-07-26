using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public partial class AnimPlayableComponent : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private bool applyRootMotion;

    private readonly List<State> states = new List<State>();
    private readonly Dictionary<object, State> statesByKey = new Dictionary<object, State>();

    private PlayableGraph graph;
    private AnimationLayerMixerPlayable layerMixer;
    private AnimPlayableLayerList layers;
    private bool hasGraph;

    public Animator Animator
    {
        get => animator;
        set
        {
            if (animator == value)
            {
                return;
            }

            animator = value;
            if (hasGraph)
            {
                RebuildGraph();
            }
            else
            {
                ApplyAnimatorSettings();
            }
        }
    }

    public bool ApplyRootMotion
    {
        get => applyRootMotion;
        set
        {
            applyRootMotion = value;
            ApplyAnimatorSettings();
        }
    }

    public bool IsGraphInitialized => hasGraph;
    public int LayerCount => layers?.Count ?? 0;
    public State CurrentState => GetCurrentState(0);

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
        return Play(new ClipTransition(clip)
        {
            FadeDuration = fadeDuration,
            Restart = restart
        });
    }

    public State Play(Transition transition)
    {
        if (transition == null)
        {
            return null;
        }

        return Play(
            GetOrCreateState(transition),
            transition.FadeDuration,
            transition.Restart);
    }

    public State Play(State state)
    {
        return Play(state, 0f);
    }

    public State Play(State state, float fadeDuration, bool restart = false)
    {
        return PlayLayer(0, state, fadeDuration, restart);
    }

    public State PlayLayer(
        int layerIndex,
        AnimationClip clip,
        float fadeDuration = 0f,
        bool restart = true,
        AvatarMask avatarMask = null)
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

    public State PlayLayer(int layerIndex, State state, float fadeDuration = 0f, bool restart = false)
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

    public void Evaluate(float deltaTime)
    {
        if (!hasGraph || layers == null || !layers.IsValid || deltaTime <= 0f)
        {
            return;
        }

        for (int i = 0; i < layers.Count; i++)
        {
            layers[i].Update(deltaTime);
        }
    }
}
