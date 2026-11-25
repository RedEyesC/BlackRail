using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrackEditor
{
    [Serializable]
    public abstract class Track : DirectableAsset
    {
        [NonSerialized, HideInInspector]
        private List<Clip> actionClips = new List<Clip>();

        [SerializeField]
        [HideInInspector]
        private bool active = true;

        [SerializeField]
        [HideInInspector]
        private bool isLocked = false;

        [SerializeField]
        private Color color = Color.white;

        public Color Color => color.a > 0.1f ? color : Color.white;

        public string Name
        {
            get => name;
            set => name = value;
        }

        public virtual string info => string.Empty;

        public virtual Group Parent
        {
            get => (Group)_parent;
            set => _parent = value;
        }

        public override bool IsCollapsed => Parent != null && Parent.IsCollapsed;

        public override bool IsActive
        {
            get => Parent != null && (Parent.IsActive && active);
            set
            {
                if (active != value)
                {
                    active = value;
                }
            }
        }

        public override bool IsLocked
        {
            get => Parent != null && (Parent.IsLocked || isLocked);
            set => isLocked = value;
        }

        public List<Clip> Clips
        {
            get => actionClips;
            set => actionClips = value;
        }

        public override float StartTime => 0;

        public override float EndTime => Parent != null ? Parent.EndTime : 0;


        public virtual float ShowHeight => 30f;

        #region 增删

        public Clip AddAction(Type type, float time)
        {
            if (!typeof(Clip).IsAssignableFrom(type) || type.IsAbstract || !CanAttach(type, GetType()))
            {
                return null;
            }

            var category = type.RTGetAttribute<CategoryAttribute>(true);
            if (category != null && Clips.Count == 0)
            {
                Name = category.category + " Track";
            }

            return InsertClip((Clip)Activator.CreateInstance(type), time);
        }

        public void DeleteAction(Clip clip)
        {
            if (Clips.Remove(clip))
            {
                clip.Parent = null;
            }
        }

        public Clip PasteClip(Clip clip, float time)
        {
            return CanAttach(clip.GetType(), GetType()) ? InsertClip(TimelineJson.Clone(clip), time) : null;
        }

        private Clip InsertClip(Clip clip, float time)
        {
            clip.Parent = this;
            clip.StartTime = time;
            var next = Clips.FirstOrDefault(other => !other.IsSignal && other.StartTime > clip.StartTime);
            if (next != null && !clip.IsSignal)
            {
                clip.EndTime = Mathf.Min(clip.EndTime, next.StartTime);
            }

            Clips.Add(clip);
            SortClips();
            return clip;
        }

        public void SortClips()
        {
            for (var i = 1; i < Clips.Count; i++)
            {
                if (Clips[i].StartTime < Clips[i - 1].StartTime)
                {
                    Clips = Clips.OrderBy(clip => clip.StartTime).ToList();
                    return;
                }
            }
        }

        #endregion
    }
}
