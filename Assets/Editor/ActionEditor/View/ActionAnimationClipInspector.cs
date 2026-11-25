using TrackEditor;
using UnityEngine;

namespace GameFramework.Action
{
    [CustomInspectors(typeof(ActionAnimationClip), true)]
    public sealed class ActionAnimationClipInspector : ClipInspector<ActionAnimationClip>
    {
        protected override bool ShowClipType => false;

        public override void OnInspectorGUI()
        {
            var previousAnim = action.anim;
            base.OnInspectorGUI();

            var anim = action.anim;
            if (anim.guid == previousAnim.guid && anim.fileId == previousAnim.fileId)
            {
                return;
            }

            if (AssetStorage.Resolve(anim, typeof(AnimationClip)) is AnimationClip animation)
            {
                action.EndTime = action.StartTime + animation.length;
            }
        }
    }
}
