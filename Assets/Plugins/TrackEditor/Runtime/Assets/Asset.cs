using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrackEditor
{
    [Serializable]
    public abstract class Asset : DirectableAsset, IDirector
    {
        [NonSerialized, HideInInspector] public List<Group> groups = new List<Group>();
        [SerializeField] private float length = 5f;

        private readonly List<DirectableAsset> orderedItems = new List<DirectableAsset>();
        public IReadOnlyList<DirectableAsset> directables => orderedItems;

        public float Length
        {
            get => length;
            set => length = Mathf.Max(0.1f, value);
        }

        public Group AddGroup(Type type)
        {
            if (!typeof(Group).IsAssignableFrom(type) || type.IsAbstract || !CanAttach(type, GetType()))
            {
                return null;
            }

            var group = (Group)Activator.CreateInstance(type);
            group.Name = type.RTGetAttribute<NameAttribute>(true)?.name ?? "Group";
            group.Parent = this;
            groups.Add(group);
            return group;
        }

        public void DeleteGroup(Group group)
        {
            if (groups.Remove(group))
            {
                group.Parent = null;
            }
        }

        public Group PasteGroup(Group group)
        {
            if (!CanAttach(group.GetType(), GetType()))
            {
                return null;
            }

            var copy = TimelineJson.Clone(group);
            copy.Parent = this;
            groups.Add(copy);
            return copy;
        }

        public void Validate()
        {
            if (float.IsNaN(length) || float.IsInfinity(length))
            {
                throw new InvalidOperationException("The timeline length must be finite.");
            }

            Length = length;
            orderedItems.Clear();
            foreach (var group in groups)
            {
                CheckAttachment(group, this);
                group.Parent = this;
                orderedItems.Add(group);
                foreach (var track in group.Tracks)
                {
                    CheckAttachment(track, group);
                    track.Parent = group;
                    track.SortClips();
                    orderedItems.Add(track);
                    foreach (var clip in track.Clips)
                    {
                        CheckAttachment(clip, track);
                        if (float.IsNaN(clip.StartTime) || float.IsInfinity(clip.EndTime) || float.IsNaN(clip.EndTime)
                            || clip.StartTime < 0 || clip.Length < 0)
                        {
                            throw new InvalidOperationException($"Clip '{clip.name}' has an invalid time range.");
                        }

                        clip.Parent = track;
                        orderedItems.Add(clip);
                    }
                }
            }
        }

        private static void CheckAttachment(DirectableAsset item, DirectableAsset parent)
        {
            if (item == null || !CanAttach(item.GetType(), parent.GetType()))
            {
                throw new InvalidOperationException($"Invalid child object under '{parent.name}'.");
            }
        }
    }
}
