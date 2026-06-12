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
        private AnimPlayableComponent _animator;
        private string _animClipName;

        public override void Update(float time, float previousTime)
        {
            if (_animator != null && _animClipName != null)
            {
                Preview(_animClipName, time);
            }
        }

        public override void Enter()
        {
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
                    AnimationClip _animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        "Assets/RawData/Anim/HeavyLance/strafe_run_frontL45.anim"
                    );

                    _animClipName = _animationClip.name;

                    _animator.AddClip(_animationClip, _animClipName);
                }
            }
        }

        /// <param name="animationClip"></param>
        /// <param name="gameObject"></param>
        /// <param name="currentTime"></param>
        public void Preview(string animClipName, float currentTime)
        {
            _animator.Sample(animClipName, currentTime);
        }
    }
}
