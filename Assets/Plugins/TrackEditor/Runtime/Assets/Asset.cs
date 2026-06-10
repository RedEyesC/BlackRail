using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TrackEditor
{
    [Serializable]
    public abstract partial class Asset : DirectableAsset, IDirector
    {
        [HideInInspector]
        public List<Group> groups = new List<Group>();

        [SerializeField]
        private float length = 5f;

        [SerializeField]
        private float viewTimeMin = 0f;

        [SerializeField]
        private float viewTimeMax = 5f;

        public float Length
        {
            get => length;
            set => length = Mathf.Max(value, 0.1f);
        }

        public float ViewTimeMin
        {
            get => viewTimeMin;
            set
            {
                if (ViewTimeMax > 0)
                    viewTimeMin = Mathf.Max(0, Mathf.Min(value, ViewTimeMax - 0.25f));
            }
        }

        public float ViewTimeMax
        {
            get => viewTimeMax;
            set => viewTimeMax = Mathf.Max(value, ViewTimeMin + 0.25f, 0);
        }

        public float MaxTime => Mathf.Max(ViewTimeMax, Length);
        public float ViewTime => ViewTimeMax - ViewTimeMin;

        public List<DirectableAsset> directables { get; private set; }

        public T AddGroup<T>(IAssetStorage storage)
            where T : Group, new()
        {
            var newGroup = CreateInstance<T>();
            newGroup.Name = "Group";
            newGroup.Parent = this;
            groups.Add(newGroup);
            CreateUtilities.SaveAssetIntoObject(newGroup, this, storage);
            DirectorUtility.selectedObject = newGroup;

            return newGroup;
        }

        public Group AddGroup(Type type, IAssetStorage storage)
        {
            var catAtt = type.GetCustomAttributes(typeof(CategoryAttribute), true).FirstOrDefault() as CategoryAttribute;
            var newGroup = CreateInstance(type) as Group;

            if (newGroup != null)
            {
                newGroup.Name = "Group";
                newGroup.Parent = this;
                groups.Add(newGroup);
                CreateUtilities.SaveAssetIntoObject(newGroup, this, storage);
                DirectorUtility.selectedObject = newGroup;
            }

            return newGroup;
        }

        public void DeleteGroup(Group group)
        {
            groups.Remove(group);
        }

        public Group PasteGroup(Group group, IAssetStorage storage)
        {
            var newGroup = Instantiate(group);
            if (newGroup != null)
            {
                newGroup.Parent = this;
                groups.Add(newGroup);
                CreateUtilities.SaveAssetIntoObject(newGroup, this, storage);
                newGroup.Tracks.Clear();
                foreach (var track in group.Tracks)
                {
                    newGroup.PasteTrack(track, storage);
                }
            }

            return newGroup;
        }

        public static Asset Load(Object source, IAssetStorage storage)
        {
            if (!storage.CanLoad(source))
            {
                return null;
            }

            var asset = storage.Load(source);
            return asset;
        }

        public static void Save(Asset asset, Object source, IAssetStorage storage)
        {
            if (asset == null)
            {
                return;
            }

            storage.Save(asset, source);
        }

        public static void SaveAssetIntoObject(Object childAsset, Object masterAsset, IAssetStorage storage)
        {
            storage.SaveAssetIntoObject(childAsset, masterAsset);
        }
    }
}
