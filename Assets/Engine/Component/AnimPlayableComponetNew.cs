using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ZZZ
{

    public class AnimPlayableComponetNew : MonoBehaviour
    {
        [Serializable]
        public class MovementClips
        {
            public AnimationClip idle;
            public AnimationClip walk;
            public AnimationClip run;
            public AnimationClip sprint;
            public AnimationClip walkEnd;
            public AnimationClip runEnd;
            public AnimationClip turnRun;
            public AnimationClip runStartEnd;
        }

        [Serializable]
        public class NamedClip
        {
            public string name;
            public AnimationClip clip;
        }

        [Serializable]
        public class ActionPlaySettings
        {
            public float fadeInTime = 0.08f;
            public float fadeOutTime = 0.12f;
            [Range(0.05f, 1f)] public float exitNormalizedTime = 0.9f;
            public float speed = 1f;
            public int priority = 100;
            public bool lockLocomotion;
            public bool allowRotation;
            public bool interruptCurrent = true;
            public bool applyFootIK = true;
        }

        public struct BlendSample
        {
            public string animationName;
            public float weight;
        }

        public struct PlaybackSnapshot
        {
            public string primaryLocomotionName;
            public string secondaryLocomotionName;
            public float primaryLocomotionWeight;
            public float secondaryLocomotionWeight;
            public string actionName;
            public float actionWeight;
            public float actionNormalizedTime;
            public bool isActionPlaying;
            public bool locomotionLocked;
            public float movementValue;
        }

        private enum LocomotionSlot
        {
            Idle = 0,
            Walk = 1,
            Run = 2,
            Sprint = 3
        }

        private enum ActionSource
        {
            None,
            LocomotionStop,
            LocomotionTurn,
            Gameplay
        }

        [Header("Runtime clips")]
        [SerializeField] private MovementClips defaultClips;
        [SerializeField] private List<NamedClip> defaultActionClips = new List<NamedClip>();
        [SerializeField] private bool initializeOnAwake;

        [Header("Input")]
        [SerializeField] private bool runInputMeansSprint = true;
        [SerializeField] private float inputDeadZone = 0.01f;

        [Header("Blend")]
        [SerializeField] private float movementDampTime = 0.35f;
        [SerializeField] private float walkValue = 1f;
        [SerializeField] private float runValue = 2f;
        [SerializeField] private float sprintValue = 3f;
        [SerializeField] private float runStopThreshold = 2.4f;

        [Header("Built-in actions")]
        [SerializeField] private ActionPlaySettings stopSettings = new ActionPlaySettings
        {
            fadeInTime = 0.08f,
            fadeOutTime = 0.12f,
            exitNormalizedTime = 0.85f,
            priority = 10,
            lockLocomotion = false,
            allowRotation = false,
            interruptCurrent = false
        };

        [SerializeField] private ActionPlaySettings turnSettings = new ActionPlaySettings
        {
            fadeInTime = 0.02f,
            fadeOutTime = 0.12f,
            exitNormalizedTime = 0.9f,
            priority = 20,
            lockLocomotion = false,
            allowRotation = true,
            interruptCurrent = false
        };

        [Header("Rotation")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private float rotationSmoothTime = 0.08f;
        [SerializeField] private float sharpTurnAngle = 135f;
        [SerializeField] private float turnRotationFreezeNormalizedTime = 0.08f;

        [Header("Root motion")]
        [SerializeField] private bool applyRootMotion = true;
        [SerializeField] private float rootMotionMultiplier = 1f;
        [SerializeField] private float gravity = -9f;
        [SerializeField] private float groundedVerticalSpeed = -2f;

        private readonly Dictionary<string, AnimationClip> actionClipMap = new Dictionary<string, AnimationClip>();
        private readonly AnimationClipPlayable[] locomotionPlayables = new AnimationClipPlayable[4];
        private readonly string[] locomotionNames = new string[4];
        private readonly float[] locomotionThresholds = new float[4];
        private readonly float[] locomotionWeights = new float[4];

        private Animator animator;
        private PlayableGraph graph;
        private AnimationMixerPlayable rootMixer;
        private AnimationMixerPlayable locomotionMixer;
        private Playable actionRootPlayable;
        private AnimationMixerPlayable actionBlendMixer;
        private AnimationClipPlayable[] actionBlendPlayables = new AnimationClipPlayable[0];
        private string[] actionBlendNames = new string[0];
        private float[] actionBlendWeights = new float[0];
        private AnimationClip currentActionClip;
        private ActionPlaySettings currentActionSettings;
        private ActionSource currentActionSource;
        private string currentActionName;
        private float currentActionLength;
        private float currentActionTime;
        private float currentActionWeight;
        private float movementValue;
        private float rotationVelocity;
        private float verticalSpeed;
        private bool hasGraph;
        private bool actionEnding;
        private bool wasMoving;
        private bool shouldWalk;

        public bool IsInitialized => hasGraph;
        public bool IsActionPlaying => currentActionSource != ActionSource.None;
        public bool IsLocomotionLocked => IsActionPlaying && currentActionSettings != null && currentActionSettings.lockLocomotion;
        public float MovementValue => movementValue;
        public string CurrentActionName => currentActionName;

        public event Action<string> ActionStarted;
        public event Action<string> ActionCompleted;
        public event Action<string> ActionInterrupted;

        private void Awake()
        {
            animator = GetComponent<Animator>();

            if (cameraRoot == null && Camera.main != null)
            {
                cameraRoot = Camera.main.transform;
            }

            if (initializeOnAwake && defaultClips != null)
            {
                Initialize(defaultClips, defaultActionClips);
            }
        }


        private void OnDestroy()
        {
            DestroyGraph();
        }

        public void Initialize(MovementClips clips)
        {
            Initialize(clips, defaultActionClips);
        }

        public void Initialize(MovementClips clips, IEnumerable<NamedClip> actionClips)
        {
            defaultClips = clips;
            RegisterActionClips(actionClips);
            RebuildGraph();
        }

        public void Initialize(
            AnimationClip idle,
            AnimationClip walk,
            AnimationClip run,
            AnimationClip sprint,
            AnimationClip walkEnd,
            AnimationClip runEnd,
            AnimationClip turnRun,
            AnimationClip runStartEnd = null)
        {
            Initialize(new MovementClips
            {
                idle = idle,
                walk = walk,
                run = run,
                sprint = sprint,
                walkEnd = walkEnd,
                runEnd = runEnd,
                turnRun = turnRun,
                runStartEnd = runStartEnd
            });
        }

        public void SetClips(MovementClips clips)
        {
            Initialize(clips);
        }

        public void RegisterActionClips(IEnumerable<NamedClip> clips)
        {
            if (clips == null)
            {
                return;
            }

            foreach (NamedClip entry in clips)
            {
                if (entry == null)
                {
                    continue;
                }

                RegisterActionClip(entry.name, entry.clip);
            }
        }

        public void RegisterActionClip(string animationName, AnimationClip clip)
        {
            if (string.IsNullOrEmpty(animationName) || clip == null)
            {
                return;
            }

            actionClipMap[animationName] = clip;
        }

        public bool TryPlayAction(string animationName, ActionPlaySettings settings)
        {
            if (string.IsNullOrEmpty(animationName))
            {
                return false;
            }

            if (!actionClipMap.TryGetValue(animationName, out AnimationClip clip))
            {
                Debug.LogWarning($"Action clip '{animationName}' is not registered.", this);
                return false;
            }

            return TryPlayAction(animationName, clip, settings, ActionSource.Gameplay);
        }

        public bool TryPlayAction(AnimationClip clip, string animationName, ActionPlaySettings settings)
        {
            return TryPlayAction(animationName, clip, settings, ActionSource.Gameplay);
        }

        public bool TryPlayActionBlend(string actionName, IList<BlendSample> samples, ActionPlaySettings settings)
        {
            if (samples == null || samples.Count == 0)
            {
                return false;
            }

            return TryPlayActionBlend(actionName, samples, settings, ActionSource.Gameplay);
        }

        public void StopAction(float fadeOutTime = -1f)
        {
            if (!IsActionPlaying)
            {
                return;
            }

            if (fadeOutTime >= 0f && currentActionSettings != null)
            {
                currentActionSettings.fadeOutTime = fadeOutTime;
            }

            actionEnding = true;
        }

        public PlaybackSnapshot GetPlaybackSnapshot()
        {
            int first = -1;
            int second = -1;

            for (int i = 0; i < locomotionWeights.Length; i++)
            {
                if (first < 0 || locomotionWeights[i] > locomotionWeights[first])
                {
                    second = first;
                    first = i;
                }
                else if (second < 0 || locomotionWeights[i] > locomotionWeights[second])
                {
                    second = i;
                }
            }

            return new PlaybackSnapshot
            {
                primaryLocomotionName = first >= 0 ? locomotionNames[first] : string.Empty,
                secondaryLocomotionName = second >= 0 ? locomotionNames[second] : string.Empty,
                primaryLocomotionWeight = first >= 0 ? locomotionWeights[first] * (1f - currentActionWeight) : 0f,
                secondaryLocomotionWeight = second >= 0 ? locomotionWeights[second] * (1f - currentActionWeight) : 0f,
                actionName = currentActionName,
                actionWeight = currentActionWeight,
                actionNormalizedTime = GetActionNormalizedTime(),
                isActionPlaying = IsActionPlaying,
                locomotionLocked = IsLocomotionLocked,
                movementValue = movementValue
            };
        }

        public int GetBlendSamples(BlendSample[] samples)
        {
            if (samples == null)
            {
                return 0;
            }

            int count = 0;
            float locomotionRootWeight = 1f - currentActionWeight;
            for (int i = 0; i < locomotionWeights.Length && count < samples.Length; i++)
            {
                if (locomotionWeights[i] <= 0f)
                {
                    continue;
                }

                samples[count++] = new BlendSample
                {
                    animationName = locomotionNames[i],
                    weight = locomotionWeights[i] * locomotionRootWeight
                };
            }

            if (currentActionWeight > 0f && actionBlendNames.Length > 0)
            {
                for (int i = 0; i < actionBlendNames.Length && count < samples.Length; i++)
                {
                    if (actionBlendWeights[i] <= 0f)
                    {
                        continue;
                    }

                    samples[count++] = new BlendSample
                    {
                        animationName = actionBlendNames[i],
                        weight = actionBlendWeights[i] * currentActionWeight
                    };
                }

                return count;
            }

            if (currentActionWeight > 0f && count < samples.Length)
            {
                samples[count++] = new BlendSample
                {
                    animationName = currentActionName,
                    weight = currentActionWeight
                };
            }

            return count;
        }

        private void RebuildGraph()
        {
            DestroyGraph();

            if (defaultClips == null || defaultClips.idle == null)
            {
                Debug.LogWarning($"{nameof(AnimPlayableComponetNew)} needs at least an idle clip.", this);
                return;
            }

            animator.applyRootMotion = applyRootMotion;
            SetupLocomotionMetadata();

            graph = PlayableGraph.Create($"{name}_PlayableMovement");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            rootMixer = AnimationMixerPlayable.Create(graph, 2);
            locomotionMixer = AnimationMixerPlayable.Create(graph, locomotionPlayables.Length);

            graph.Connect(locomotionMixer, 0, rootMixer, 0);
            rootMixer.SetInputWeight(0, 1f);
            rootMixer.SetInputWeight(1, 0f);

            CreateLocomotionInput((int)LocomotionSlot.Idle, defaultClips.idle);
            CreateLocomotionInput((int)LocomotionSlot.Walk, defaultClips.walk);
            CreateLocomotionInput((int)LocomotionSlot.Run, defaultClips.run);
            CreateLocomotionInput((int)LocomotionSlot.Sprint, defaultClips.sprint);

            var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            output.SetSourcePlayable(rootMixer);

            graph.Play();
            hasGraph = true;
            ApplyLocomotionWeights(0f);
        }

        private void SetupLocomotionMetadata()
        {
            locomotionNames[(int)LocomotionSlot.Idle] = GetClipName(defaultClips.idle, "Idle");
            locomotionNames[(int)LocomotionSlot.Walk] = GetClipName(defaultClips.walk, "Walk");
            locomotionNames[(int)LocomotionSlot.Run] = GetClipName(defaultClips.run, "Run");
            locomotionNames[(int)LocomotionSlot.Sprint] = GetClipName(defaultClips.sprint, "Sprint");

            locomotionThresholds[(int)LocomotionSlot.Idle] = 0f;
            locomotionThresholds[(int)LocomotionSlot.Walk] = walkValue;
            locomotionThresholds[(int)LocomotionSlot.Run] = runValue;
            locomotionThresholds[(int)LocomotionSlot.Sprint] = sprintValue;
        }

        private void CreateLocomotionInput(int index, AnimationClip clip)
        {
            if (clip == null)
            {
                return;
            }

            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(true);
            playable.SetTime(0);
            playable.SetDuration(clip.length);
            graph.Connect(playable, 0, locomotionMixer, index);
            locomotionMixer.SetInputWeight(index, index == 0 ? 1f : 0f);
            locomotionPlayables[index] = playable;
        }

        private void Update()
        {
            if (!hasGraph)
            {
                return;
            }

            Vector2 moveInput = new Vector2(0,0);
            bool hasMoveInput = moveInput.sqrMagnitude > inputDeadZone * inputDeadZone;


            UpdateMovementValue(hasMoveInput, moveInput);
            UpdateAction(hasMoveInput, moveInput);

            if (!IsActionBlockingLocomotionLogic())
            {
                UpdateLocomotionActions(hasMoveInput, moveInput);
            }
            else if (currentActionSettings.allowRotation && hasMoveInput)
            {
                RotateToInput(moveInput);
            }

            UpdateRootWeights();
            wasMoving = hasMoveInput;
        }

        private void UpdateMovementValue(bool hasMoveInput, Vector2 moveInput)
        {
            float targetValue = 0f;

            if (hasMoveInput && !IsLocomotionLocked)
            {
                if (shouldWalk)
                {
                    targetValue = walkValue;
                }
                else if (runInputMeansSprint)
                {
                    targetValue = sprintValue;
                }
                else
                {
                    targetValue = runValue;
                }

                targetValue *= Mathf.Clamp01(moveInput.sqrMagnitude);
            }

            float lerp = movementDampTime <= 0f ? 1f : 1f - Mathf.Exp(-Time.deltaTime / movementDampTime);
            movementValue = Mathf.Lerp(movementValue, targetValue, lerp);
            ApplyLocomotionWeights(movementValue);
        }

        private void ApplyLocomotionWeights(float value)
        {
            ClearLocomotionWeights();

            if (value <= walkValue)
            {
                locomotionWeights[(int)LocomotionSlot.Idle] = Mathf.InverseLerp(walkValue, 0f, value);
                locomotionWeights[(int)LocomotionSlot.Walk] = Mathf.InverseLerp(0f, walkValue, value);
                NormalizeAndWriteLocomotionWeights(value);
                return;
            }

            if (value <= runValue)
            {
                locomotionWeights[(int)LocomotionSlot.Walk] = Mathf.InverseLerp(runValue, walkValue, value);
                locomotionWeights[(int)LocomotionSlot.Run] = Mathf.InverseLerp(walkValue, runValue, value);
                NormalizeAndWriteLocomotionWeights(value);
                return;
            }

            locomotionWeights[(int)LocomotionSlot.Run] = Mathf.InverseLerp(sprintValue, runValue, value);
            locomotionWeights[(int)LocomotionSlot.Sprint] = Mathf.InverseLerp(runValue, sprintValue, value);
            NormalizeAndWriteLocomotionWeights(value);
        }

        private void ClearLocomotionWeights()
        {
            for (int i = 0; i < locomotionWeights.Length; i++)
            {
                locomotionWeights[i] = 0f;
            }
        }

        private void NormalizeAndWriteLocomotionWeights(float movement)
        {
            float validWeightSum = 0f;
            for (int i = 0; i < locomotionPlayables.Length; i++)
            {
                if (!locomotionPlayables[i].IsValid())
                {
                    locomotionWeights[i] = 0f;
                    continue;
                }

                validWeightSum += locomotionWeights[i];
            }

            if (validWeightSum <= 0f)
            {
                int fallbackIndex = GetNearestValidLocomotionIndex(movement);
                if (fallbackIndex >= 0)
                {
                    locomotionWeights[fallbackIndex] = 1f;
                    validWeightSum = 1f;
                }
            }

            for (int i = 0; i < locomotionPlayables.Length; i++)
            {
                float normalizedWeight = validWeightSum <= 0f ? 0f : locomotionWeights[i] / validWeightSum;
                locomotionWeights[i] = normalizedWeight;

                if (locomotionPlayables[i].IsValid())
                {
                    locomotionMixer.SetInputWeight(i, normalizedWeight);
                }
            }
        }

        private int GetNearestValidLocomotionIndex(float movement)
        {
            int bestIndex = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < locomotionPlayables.Length; i++)
            {
                if (!locomotionPlayables[i].IsValid())
                {
                    continue;
                }

                float distance = Mathf.Abs(movement - locomotionThresholds[i]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private void UpdateLocomotionActions(bool hasMoveInput, Vector2 moveInput)
        {
            if (wasMoving && !hasMoveInput)
            {
                AnimationClip stopClip = movementValue >= runStopThreshold ? defaultClips.runEnd : defaultClips.walkEnd;
                string stopName = movementValue >= runStopThreshold ? "Run_End" : "Walk_End";
                TryPlayAction(stopName, stopClip, stopSettings, ActionSource.LocomotionStop);
                return;
            }

            if (!hasMoveInput)
            {
                return;
            }

            RotateToInput(moveInput);

            if (!shouldWalk && IsSharpTurn(moveInput))
            {
                TryPlayAction("TurnRun", defaultClips.turnRun, turnSettings, ActionSource.LocomotionTurn);
            }
        }

        private void UpdateAction(bool hasMoveInput, Vector2 moveInput)
        {
            if (!IsActionPlaying)
            {
                return;
            }

            currentActionTime += Time.deltaTime * Mathf.Max(0f, currentActionSettings.speed);
            float normalizedTime = GetActionNormalizedTime();

            if (currentActionSource == ActionSource.LocomotionTurn)
            {
                if (hasMoveInput && normalizedTime >= turnRotationFreezeNormalizedTime)
                {
                    RotateToInput(moveInput);
                }
            }
            else if (currentActionSettings.allowRotation && hasMoveInput)
            {
                RotateToInput(moveInput);
            }

            if (currentActionSource == ActionSource.LocomotionStop && hasMoveInput && currentActionTime > 0.08f)
            {
                actionEnding = true;
                return;
            }

            if (normalizedTime >= currentActionSettings.exitNormalizedTime)
            {
                actionEnding = true;
            }
        }

        private bool TryPlayAction(string animationName, AnimationClip clip, ActionPlaySettings settings, ActionSource source)
        {
            if (!hasGraph || clip == null)
            {
                return false;
            }

            ActionPlaySettings resolvedSettings = CloneSettings(settings);
            if (!CanInterruptCurrentAction(resolvedSettings))
            {
                return false;
            }

            InterruptCurrentActionIfNeeded();
            DestroyActionPlayable();
            ResetActionWeightForNewAction();

            currentActionClip = clip;
            currentActionName = string.IsNullOrEmpty(animationName) ? clip.name : animationName;
            currentActionLength = clip.length;
            currentActionSettings = resolvedSettings;
            currentActionSource = source;
            currentActionTime = 0f;
            actionEnding = false;

            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(resolvedSettings.applyFootIK);
            playable.SetSpeed(Mathf.Max(0f, resolvedSettings.speed));
            playable.SetTime(0);
            playable.SetDuration(clip.length);
            actionRootPlayable = playable;
            graph.Connect(actionRootPlayable, 0, rootMixer, 1);

            ActionStarted?.Invoke(currentActionName);
            return true;
        }

        private bool TryPlayActionBlend(string actionName, IList<BlendSample> samples, ActionPlaySettings settings, ActionSource source)
        {
            if (!hasGraph)
            {
                return false;
            }

            ActionPlaySettings resolvedSettings = CloneSettings(settings);
            if (!CanInterruptCurrentAction(resolvedSettings))
            {
                return false;
            }

            List<BlendSample> validSamples = new List<BlendSample>(samples.Count);
            float totalWeight = 0f;
            float longestLength = 0f;

            for (int i = 0; i < samples.Count; i++)
            {
                BlendSample sample = samples[i];
                if (sample.weight <= 0f || string.IsNullOrEmpty(sample.animationName))
                {
                    continue;
                }

                if (!actionClipMap.TryGetValue(sample.animationName, out AnimationClip clip) || clip == null)
                {
                    Debug.LogWarning($"Action clip '{sample.animationName}' is not registered.", this);
                    continue;
                }

                validSamples.Add(sample);
                totalWeight += sample.weight;
                longestLength = Mathf.Max(longestLength, clip.length);
            }

            if (validSamples.Count == 0 || totalWeight <= 0f)
            {
                return false;
            }

            InterruptCurrentActionIfNeeded();
            DestroyActionPlayable();
            ResetActionWeightForNewAction();

            actionBlendMixer = AnimationMixerPlayable.Create(graph, validSamples.Count);
            actionBlendPlayables = new AnimationClipPlayable[validSamples.Count];
            actionBlendNames = new string[validSamples.Count];
            actionBlendWeights = new float[validSamples.Count];

            for (int i = 0; i < validSamples.Count; i++)
            {
                BlendSample sample = validSamples[i];
                AnimationClip clip = actionClipMap[sample.animationName];
                float normalizedWeight = sample.weight / totalWeight;

                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(resolvedSettings.applyFootIK);
                playable.SetSpeed(Mathf.Max(0f, resolvedSettings.speed));
                playable.SetTime(0);
                playable.SetDuration(clip.length);

                graph.Connect(playable, 0, actionBlendMixer, i);
                actionBlendMixer.SetInputWeight(i, normalizedWeight);
                actionBlendPlayables[i] = playable;
                actionBlendNames[i] = sample.animationName;
                actionBlendWeights[i] = normalizedWeight;
            }

            currentActionClip = null;
            currentActionName = string.IsNullOrEmpty(actionName) ? "ActionBlend" : actionName;
            currentActionLength = longestLength;
            currentActionSettings = resolvedSettings;
            currentActionSource = source;
            currentActionTime = 0f;
            actionEnding = false;
            actionRootPlayable = actionBlendMixer;
            graph.Connect(actionRootPlayable, 0, rootMixer, 1);

            ActionStarted?.Invoke(currentActionName);
            return true;
        }

        private void ResetActionWeightForNewAction()
        {
            currentActionWeight = 0f;
            if (rootMixer.IsValid())
            {
                rootMixer.SetInputWeight(0, 1f);
                rootMixer.SetInputWeight(1, 0f);
            }
        }

        private bool CanInterruptCurrentAction(ActionPlaySettings nextSettings)
        {
            if (!IsActionPlaying)
            {
                return true;
            }

            if (!nextSettings.interruptCurrent)
            {
                return false;
            }

            return nextSettings.priority >= currentActionSettings.priority;
        }

        private ActionPlaySettings CloneSettings(ActionPlaySettings settings)
        {
            ActionPlaySettings source = settings ?? new ActionPlaySettings();
            return new ActionPlaySettings
            {
                fadeInTime = Mathf.Max(0.01f, source.fadeInTime),
                fadeOutTime = Mathf.Max(0.01f, source.fadeOutTime),
                exitNormalizedTime = Mathf.Clamp01(source.exitNormalizedTime),
                speed = Mathf.Max(0f, source.speed),
                priority = source.priority,
                lockLocomotion = source.lockLocomotion,
                allowRotation = source.allowRotation,
                interruptCurrent = source.interruptCurrent,
                applyFootIK = source.applyFootIK
            };
        }

        private void UpdateRootWeights()
        {
            float target = IsActionPlaying && !actionEnding ? 1f : 0f;
            float fadeTime = target > currentActionWeight
                ? currentActionSettings.fadeInTime
                : currentActionSettings != null ? currentActionSettings.fadeOutTime : 0.1f;
            float maxDelta = fadeTime <= 0f ? 1f : Time.deltaTime / fadeTime;

            currentActionWeight = Mathf.MoveTowards(currentActionWeight, target, maxDelta);
            rootMixer.SetInputWeight(0, 1f - currentActionWeight);
            rootMixer.SetInputWeight(1, currentActionWeight);

            if (IsActionPlaying && actionEnding && currentActionWeight <= 0f)
            {
                CompleteCurrentAction();
            }
        }

        private void CompleteCurrentAction()
        {
            string completedName = currentActionName;

            DestroyActionPlayable();
            currentActionClip = null;
            currentActionSettings = null;
            currentActionSource = ActionSource.None;
            currentActionName = string.Empty;
            currentActionLength = 0f;
            currentActionTime = 0f;
            actionEnding = false;

            if (!string.IsNullOrEmpty(completedName))
            {
                ActionCompleted?.Invoke(completedName);
            }
        }

        private void InterruptCurrentActionIfNeeded()
        {
            if (!IsActionPlaying)
            {
                return;
            }

            string interruptedName = currentActionName;
            if (!string.IsNullOrEmpty(interruptedName))
            {
                ActionInterrupted?.Invoke(interruptedName);
            }
        }

        private bool IsActionBlockingLocomotionLogic()
        {
            if (!IsActionPlaying)
            {
                return false;
            }

            if (currentActionSource == ActionSource.LocomotionStop || currentActionSource == ActionSource.LocomotionTurn)
            {
                return false;
            }

            return currentActionSettings.lockLocomotion;
        }

        private float GetActionNormalizedTime()
        {
            if (currentActionLength <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(currentActionTime / currentActionLength);
        }

        private bool IsSharpTurn(Vector2 moveInput)
        {
            Vector3 targetDirection = GetWorldMoveDirection(moveInput);
            if (targetDirection.sqrMagnitude <= 0f)
            {
                return false;
            }

            float angle = Vector3.Angle(transform.forward, targetDirection);
            return angle >= sharpTurnAngle;
        }

        private void RotateToInput(Vector2 moveInput)
        {
            Vector3 targetDirection = GetWorldMoveDirection(moveInput);
            if (targetDirection.sqrMagnitude <= 0f)
            {
                return;
            }

            float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
            float y = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, y, 0f);
        }

        private Vector3 GetWorldMoveDirection(Vector2 moveInput)
        {
            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
            if (direction.sqrMagnitude <= 0f)
            {
                return Vector3.zero;
            }

            Transform reference = cameraRoot != null ? cameraRoot : transform;
            return Quaternion.Euler(0f, reference.eulerAngles.y, 0f) * direction.normalized;
        }


        private void OnAnimatorMove()
        {
            if (!applyRootMotion || animator == null)
            {
                return;
            }

            Vector3 delta = animator.deltaPosition * rootMotionMultiplier;
            delta.y = 0f;
        }

        private void DestroyActionPlayable()
        {
            if (!hasGraph || !rootMixer.IsValid())
            {
                return;
            }

            if (actionRootPlayable.IsValid())
            {
                rootMixer.DisconnectInput(1);
            }

            for (int i = 0; i < actionBlendPlayables.Length; i++)
            {
                if (actionBlendPlayables[i].IsValid())
                {
                    if (actionBlendMixer.IsValid())
                    {
                        actionBlendMixer.DisconnectInput(i);
                    }

                    graph.DestroyPlayable(actionBlendPlayables[i]);
                }
            }

            if (actionRootPlayable.IsValid())
            {
                graph.DestroyPlayable(actionRootPlayable);
            }

            actionRootPlayable = default(Playable);
            actionBlendMixer = default(AnimationMixerPlayable);
            actionBlendPlayables = new AnimationClipPlayable[0];
            actionBlendNames = new string[0];
            actionBlendWeights = new float[0];
        }

        private void DestroyGraph()
        {
            if (graph.IsValid())
            {
                graph.Destroy();
            }

            Array.Clear(locomotionPlayables, 0, locomotionPlayables.Length);
            ClearLocomotionWeights();
            hasGraph = false;
            actionRootPlayable = default(Playable);
            actionBlendMixer = default(AnimationMixerPlayable);
            actionBlendPlayables = new AnimationClipPlayable[0];
            actionBlendNames = new string[0];
            actionBlendWeights = new float[0];
            currentActionClip = null;
            currentActionSettings = null;
            currentActionSource = ActionSource.None;
            currentActionName = string.Empty;
            currentActionLength = 0f;
            currentActionTime = 0f;
            currentActionWeight = 0f;
            actionEnding = false;
        }

        private static string GetClipName(AnimationClip clip, string fallback)
        {
            return clip != null ? clip.name : fallback;
        }
    }
}
