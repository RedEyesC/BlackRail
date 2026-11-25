using System;
using UnityEngine;

namespace TrackEditor
{
    [Serializable]
    public abstract class Clip : DirectableAsset
    {
        [SerializeField]
        private float startTime;
        [SerializeField]
        private float length = 1f;
        [SerializeField]
        private ClipType clipType;

        public virtual ClipType Type
        {
            get => clipType;
            set => clipType = value;
        }

        public bool IsSignal => Type == ClipType.Signal;

        public virtual Track Parent
        {
            get => (Track)_parent;
            set => _parent = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

        public string DisplayName => string.IsNullOrWhiteSpace(CustomName) ? info : CustomName;

        public override float StartTime
        {
            get => startTime;
            set => startTime = Mathf.Max(value, 0);
        }

        public override float EndTime
        {
            get => StartTime + Length;
            set
            {
                if (EndTime != value)
                {
                    Length = Mathf.Max(value - StartTime, 0);
                }
            }
        }

        public override bool IsActive => Parent != null ? Parent.IsActive : false;
        public override bool IsCollapsed => Parent != null && Parent.IsCollapsed;

        public override bool IsLocked => Parent != null && Parent.IsLocked;

        public virtual float Length
        {
            get => IsSignal ? 0 : length;
            set
            {
                if (!IsSignal)
                {
                    length = value;
                }
            }
        }

        public bool Crossed(float from, float to) => IsSignal && IsActive
            && (from < to ? from < StartTime && StartTime <= to : to <= StartTime && StartTime < from);

        public virtual string info
        {
            get
            {
                var nameAtt = this.GetType().RTGetAttribute<NameAttribute>(true);
                if (nameAtt != null)
                {
                    return nameAtt.name;
                }

                return this.GetType().Name.SplitCamelCase();
            }
        }

        public virtual bool isValid => false;

        public Clip GetNextClip()
        {
            return this.GetNextSibling<Clip>();
        }

        public void TryMatchSubClipLength()
        {
            if (this is ISubClipContainable)
            {
                Length = ((ISubClipContainable)this).SubClipLength / ((ISubClipContainable)this).SubClipSpeed;
            }
        }

        public void TryMatchPreviousSubClipLoop()
        {
            if (this is ISubClipContainable)
            {
                Length = (this as ISubClipContainable).GetPreviousLoopLocalTime();
            }
        }

        public void TryMatchNexSubClipLoop()
        {
            if (this is ISubClipContainable)
            {
                var targetLength = (this as ISubClipContainable).GetNextLoopLocalTime();
                var nextClip = GetNextClip();
                if (nextClip == null || StartTime + targetLength <= nextClip.StartTime)
                {
                    Length = targetLength;
                }
            }
        }

        #region Unity Editor

#if UNITY_EDITOR

        public void ShowClipGUI(Rect rect)
        {
            OnClipGUI(rect);
        }

        public void ShowClipGUIExternal(Rect left, Rect right)
        {
            OnClipGUIExternal(left, right);
        }

        protected virtual void OnClipGUI(Rect rect) { }

        protected virtual void OnClipGUIExternal(Rect left, Rect right) { }

#endif
        #endregion
    }

}
