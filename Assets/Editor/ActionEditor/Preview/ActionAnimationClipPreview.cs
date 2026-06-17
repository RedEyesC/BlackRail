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
        private string _animResPath = "Assets/Resource/Anim/";
        private AnimPlayableComponent _animator;
        private string _animClipName;
        private AnimationClip _animationClip;

        public override void SetTarget(DirectableAsset t)
        {
            base.SetTarget(t);

            var model = ModelSampler.EditModel;
            if (model != null)
            {
                _animator = model.GetComponent<AnimPlayableComponent>();
                _animator.DestroyOutput();
                _animator.DestroyGraph();
                _animator.RestStates();
                _animator.InitializeGraph();
            }

            if (_animator != null)
            {
                if (clip.resPath != null)
                {
                    _animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(_animResPath + clip.resPath + ".anim");

                    if (_animationClip == null)
                    {
                        return;
                    }

                    _animClipName = _animationClip.name;
                    clip.SubClipLength = _animationClip.length;

                    _animator.AddClip(_animationClip, _animClipName);
                }
            }
        }

        public override void Update(float time, float previousTime)
        {
            if (_animator != null && _animClipName != null)
            {
                Preview(_animClipName, time);
            }
        }

        public override void Enter() { }

        /// <param name="animationClip"></param>
        /// <param name="gameObject"></param>
        /// <param name="currentTime"></param>
        public void Preview(string animClipName, float currentTime)
        {
            _animator.Sample(animClipName, currentTime);
        }
    }
}
