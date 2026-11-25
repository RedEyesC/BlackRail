using System.Collections.Generic;
using TrackEditor;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Action
{
    [TrackEditor.CustomPreview(typeof(ActionAnimationClip))]
    public class ActionAnimationClipPreview : PreviewBase<ActionAnimationClip>
    {
        private AnimationClip animation;
        private readonly List<EditorCurveBinding> bindings = new List<EditorCurveBinding>();

        public override void SetTarget(DirectableAsset target)
        {
            base.SetTarget(target);
            animation = Context.ResolveAnimation(clip.resPath);
            clip.SetResolvedSourceLength(animation?.length);
            bindings.Clear();
            if (animation != null)
            {
                bindings.AddRange(AnimationUtility.GetCurveBindings(animation));
                bindings.AddRange(AnimationUtility.GetObjectReferenceCurveBindings(animation));
            }
        }

        public override void Update(float time, float previousTime)
        {
            if (animation != null && Context.Time >= clip.StartTime && Context.Time <= clip.EndTime)
            {
                Context.QueueAnimation(clip, animation, SampleTime(0), SampleTime(time), bindings);
            }
        }

        private float SampleTime(float localTime)
        {
            var time = clip.SubClipOffset + localTime * clip.SubClipSpeed;
            return animation.isLooping && animation.length > 0
                ? Mathf.Repeat(time, animation.length)
                : Mathf.Clamp(time, 0, animation.length);
        }
    }
}
