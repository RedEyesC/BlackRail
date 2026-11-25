using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrackEditor
{
    [Serializable]
    public class Group : DirectableAsset
    {
        [NonSerialized, HideInInspector]
        private List<Track> tracks = new List<Track>();

        [SerializeField, HideInInspector]
        private bool isCollapsed = false;

        [SerializeField, HideInInspector]
        private bool active = true;

        [SerializeField, HideInInspector]
        private bool isLocked = false;

        public virtual Asset Parent
        {
            get => (Asset)_parent;
            set => _parent = value;
        }


        public override float StartTime => 0;
        public override float EndTime => Parent?.Length ?? 0;

        public string Name
        {
            get => name;
            set => name = value;
        }

        public List<Track> Tracks
        {
            get => tracks;
            set => tracks = value;
        }

        public override bool IsActive
        {
            get => active;
            set
            {
                if (active != value)
                {
                    active = value;
                }
            }
        }

        public override bool IsCollapsed
        {
            get => isCollapsed;
            set => isCollapsed = value;
        }

        public override bool IsLocked
        {
            get => isLocked;
            set => isLocked = value;
        }

        #region 增删

        public bool CanAddTrack(Track track)
        {
            return track != null && CanAddTrackOfType(track.GetType());
        }

        public bool CanAddTrackOfType(Type type)
        {
            if (type == null || !type.IsSubclassOf(typeof(Track)) || type.IsAbstract)
            {
                return false;
            }

            if (type.IsDefined(typeof(UniqueAttribute), true) && Tracks.FirstOrDefault(t => t.GetType() == type) != null)
            {
                return false;
            }

            return CanAttach(type, GetType());
        }

        public Track AddTrack(Type type, string trackName = null)
        {
            if (!CanAddTrackOfType(type))
            {
                return null;
            }

            var track = (Track)Activator.CreateInstance(type);
            track.Name = string.IsNullOrEmpty(trackName)
                ? type.RTGetAttribute<NameAttribute>(true)?.name ?? type.Name
                : trackName;
            track.Parent = this;
            Tracks.Add(track);
            return track;
        }

        public void DeleteTrack(Track track)
        {
            if (Tracks.Remove(track))
            {
                track.Parent = null;
            }
        }

        public bool CanMoveTrack(Track track, int index)
        {
            if (track?.Parent == null || Parent == null || Parent != track.Parent.Parent
                || track.IsLocked || IsLocked || index < 0 || index > Tracks.Count)
            {
                return false;
            }

            var current = track.Parent.Tracks.IndexOf(track);
            return current >= 0 && (track.Parent == this
                ? index != current && index != current + 1
                : CanAddTrack(track));
        }

        public bool MoveTrack(Track track, int index)
        {
            if (!CanMoveTrack(track, index))
            {
                return false;
            }

            var source = track.Parent;
            var current = source.Tracks.IndexOf(track);
            if (source == this && current < index)
            {
                index--;
            }

            source.Tracks.RemoveAt(current);
            Tracks.Insert(index, track);
            track.Parent = this;
            return true;
        }

        public Track PasteTrack(Track track)
        {
            if (!CanAddTrack(track))
            {
                return null;
            }

            var copy = TimelineJson.Clone(track);
            copy.Parent = this;
            Tracks.Add(copy);
            return copy;
        }

        #endregion
    }
}
