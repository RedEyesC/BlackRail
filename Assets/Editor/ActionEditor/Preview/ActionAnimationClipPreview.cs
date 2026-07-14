using System.Collections.Generic;
using TrackEditor;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Action
{
    /// <summary>
    /// 动画预览
    /// </summary>
    [TrackEditor.CustomPreview(typeof(ActionAnimationClip))]
    public class ActionAnimationClipPreview : PreviewBase<ActionAnimationClip>
    {
        private const string AnimResPath = "Assets/Resource/Anim/";

        private GameObject _previewTarget;
        private AnimationClip _animationClip;
        private readonly List<EditorCurveBinding> _bindings = new List<EditorCurveBinding>();

        private bool _hasPreviousRootSample;
        private Vector3 _previousClipRootPosition;
        private Quaternion _previousClipRootRotation;

        public override void SetTarget(DirectableAsset t)
        {
            base.SetTarget(t);

            ResetRootSample();
            _previewTarget = ModelSampler.EditModel;
            _animationClip = LoadAnimationClip();

            _bindings.Clear();
            if (_previewTarget != null && _animationClip != null)
            {
                CollectAnimatableBindings(_previewTarget.transform);
                clip.SubClipLength = _animationClip.length;
            }
        }

        public override void Enter()
        {
            ResetRootSample();
            StartAnimationModeIfNeeded();
        }

        public override void Exit()
        {
            StopPreview();
        }

        public override void ReverseEnter()
        {
            ResetRootSample();
            StartAnimationModeIfNeeded();
        }

        public override void Reverse()
        {
            StopPreview();
        }

        public override void Update(float time, float previousTime)
        {
            if (_previewTarget == null || _animationClip == null)
            {
                return;
            }

            Preview(GetClipSampleTime(time));
        }

        public void Preview(float sampleTime)
        {
            if (_previewTarget == null || _animationClip == null || Application.isPlaying)
            {
                return;
            }

            var sceneRootPosition = _previewTarget.transform.position;
            var sceneRootRotation = _previewTarget.transform.rotation;

            StartAnimationModeIfNeeded();
            AnimationMode.BeginSampling();

            try
            {
                foreach (var binding in _bindings)
                {
                    AnimationMode.AddEditorCurveBinding(_previewTarget, binding);
                }

                AnimationMode.SampleAnimationClip(_previewTarget, _animationClip, sampleTime);
                ApplyRootDelta(sceneRootPosition, sceneRootRotation);
            }
            finally
            {
                AnimationMode.EndSampling();
            }
        }

        private AnimationClip LoadAnimationClip()
        {
            if (clip == null || string.IsNullOrEmpty(clip.resPath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimResPath + clip.resPath + ".anim");
        }

        private float GetClipSampleTime(float localTime)
        {
            if (_animationClip == null)
            {
                return 0f;
            }

            var speed = Mathf.Max(0.0001f, clip.SubClipSpeed);
            var sampleTime = clip.SubClipOffset + localTime * speed;
            var length = Mathf.Max(0f, _animationClip.length);

            if (_animationClip.isLooping && length > 0f)
            {
                sampleTime = Mathf.Repeat(sampleTime, length);
            }
            else
            {
                sampleTime = Mathf.Clamp(sampleTime, 0f, length);
            }

            return sampleTime;
        }

        private void ApplyRootDelta(Vector3 sceneRootPosition, Quaternion sceneRootRotation)
        {
            var sampledRootPosition = _previewTarget.transform.position;
            var sampledRootRotation = _previewTarget.transform.rotation;

            if (_hasPreviousRootSample)
            {
                var deltaPosition = Quaternion.Inverse(_previousClipRootRotation) * (sampledRootPosition - _previousClipRootPosition);
                var deltaRotation = Quaternion.Inverse(_previousClipRootRotation) * sampledRootRotation;

                _previewTarget.transform.SetPositionAndRotation(
                    sceneRootPosition + sceneRootRotation * deltaPosition,
                    sceneRootRotation * deltaRotation
                );
            }
            else
            {
                _previewTarget.transform.SetPositionAndRotation(sceneRootPosition, sceneRootRotation);
                _hasPreviousRootSample = true;
            }

            _previousClipRootPosition = sampledRootPosition;
            _previousClipRootRotation = sampledRootRotation;
        }

        private void CollectAnimatableBindings(Transform root)
        {
            foreach (var binding in AnimationUtility.GetAnimatableBindings(root.gameObject, _previewTarget))
            {
                if (IsPreviewBinding(binding))
                {
                    _bindings.Add(binding);
                }
            }

            for (int i = 0; i < root.childCount; i++)
            {
                CollectAnimatableBindings(root.GetChild(i));
            }
        }

        private static bool IsPreviewBinding(EditorCurveBinding binding)
        {
            return binding.propertyName.Contains("Local")
                || binding.propertyName.StartsWith("m_Local")
                || binding.propertyName == "RootT.x"
                || binding.propertyName == "RootT.y"
                || binding.propertyName == "RootT.z"
                || binding.propertyName == "RootQ.x"
                || binding.propertyName == "RootQ.y"
                || binding.propertyName == "RootQ.z"
                || binding.propertyName == "RootQ.w";
        }

        private static void StartAnimationModeIfNeeded()
        {
            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
            }
        }

        private void StopPreview()
        {
            ResetRootSample();

            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private void ResetRootSample()
        {
            _hasPreviousRootSample = false;
            _previousClipRootPosition = Vector3.zero;
            _previousClipRootRotation = Quaternion.identity;
        }
    }
}
