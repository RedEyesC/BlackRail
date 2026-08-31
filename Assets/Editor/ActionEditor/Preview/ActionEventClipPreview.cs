using System.Collections.Generic;
using TrackEditor;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Action
{
    /// <summary>
    /// 动画预览
    /// </summary>
    [TrackEditor.CustomPreview(typeof(ActionEventClip))]
    public class ActionEventClipPreview : PreviewBase<ActionEventClip>
    {
        private GameObject _previewTarget;

        public override void SetTarget(DirectableAsset t)
        {
            base.SetTarget(t);

            _previewTarget = ModelSampler.EditModel;
        }

        public override void Enter()
        {
            StartAnimationModeIfNeeded();
        }

        public override void Exit()
        {
            StopPreview();
        }

        public override void ReverseEnter()
        {
            StartAnimationModeIfNeeded();
        }

        public override void Reverse()
        {
            StopPreview();
        }

        public override void Update(float time, float previousTime)
        {
            if (_previewTarget == null)
            {
                return;
            }
        }

        private static void StartAnimationModeIfNeeded()
        {
            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
            }
        }

        private void StopPreview() { }
    }
}
