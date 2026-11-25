using System;
using System.Linq;
using UnityEngine;

namespace TrackEditor
{
    [Serializable]
    public abstract class DirectableAsset : IData
    {
        [NonSerialized] internal DirectableAsset _parent;
        [SerializeField, HideInInspector] private string displayName;

        public string name
        {
            get => displayName ?? GetType().Name;
            set => displayName = value;
        }

        protected string CustomName => displayName;

        public DirectableAsset parent => _parent;
        public virtual bool IsActive { get; set; }
        public virtual bool IsLocked { get; set; }
        public virtual bool IsCollapsed { get; set; }
        public virtual float StartTime { get; set; }
        public virtual float EndTime { get; set; }

        public static bool CanAttach(Type type, Type parentType) =>
            type.RTGetAttribute<AttachableAttribute>(true)?.Types?.Any(parent => parent.IsAssignableFrom(parentType)) == true;
    }
}
