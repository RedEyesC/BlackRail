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

        [HideInInspector, NonSerialized]
        Track[] m_CacheOutputTracks;

        [NonSerialized]
        private IAssetStorage storage;

        [NonSerialized]
        private UnityEngine.Object storageSource;

        public IAssetStorage Storage => storage;

        public UnityEngine.Object StorageSource => storageSource;

        public static IAssetStorage DefaultStorage { get; } = new AssetStorage();

        internal void SetStorage(IAssetStorage value, UnityEngine.Object source)
        {
            storage = value;
            storageSource = source;
        }

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
                    viewTimeMin = Mathf.Min(value, ViewTimeMax - 0.25f);
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

        public T AddGroup<T>()
            where T : Group, new()
        {
            var newGroup = CreateInstance<T>();
            newGroup.Name = "New Group";
            newGroup.Parent = this;
            groups.Add(newGroup);
            CreateUtilities.SaveAssetIntoObject(newGroup, this);
            DirectorUtility.selectedObject = newGroup;

            return newGroup;
        }

        public Group AddGroup(Type type)
        {
            var catAtt = type.GetCustomAttributes(typeof(CategoryAttribute), true).FirstOrDefault() as CategoryAttribute;
            var newGroup = CreateInstance(type) as Group;

            if (newGroup != null)
            {
                newGroup.Name = "New Group";
                newGroup.Parent = this;
                groups.Add(newGroup);
                CreateUtilities.SaveAssetIntoObject(newGroup, this);
                DirectorUtility.selectedObject = newGroup;
            }

            return newGroup;
        }

        public void DeleteGroup(Group group)
        {
            groups.Remove(group);
        }

        public Group PasteGroup(Group group)
        {
            var newGroup = Instantiate(group);
            if (newGroup != null)
            {
                newGroup.Parent = this;
                groups.Add(newGroup);
                CreateUtilities.SaveAssetIntoObject(newGroup, this);
                newGroup.Tracks.Clear();
                foreach (var track in group.Tracks)
                {
                    newGroup.PasteTrack(track);
                }
            }

            return newGroup;
        }

        public override void SaveToAssets()
        {
#if UNITY_EDITOR
            Save(this, StorageSource, Storage);
#endif
        }

        public static Asset Load(Object source, IAssetStorage storage)
        {
            if (!storage.CanLoad(source))
            {
                return null;
            }

            var asset = storage.Load(source);
            Bind(asset, storage, source);
            return asset;
        }

        public static void Save(Asset asset, Object source, IAssetStorage storage)
        {
            if (asset == null)
            {
                return;
            }

            source = source != null ? source : asset.StorageSource;
            storage.Save(asset, source);
        }

        public static void SaveAssetIntoObject(Object childAsset, Object masterAsset)
        {
            var root = GetRoot(masterAsset);
            var storage = root != null ? root.Storage : null;
            if (storage == null)
            {
                return;
            }

            storage.SaveAssetIntoObject(childAsset, masterAsset);
        }

        internal static void Bind(Asset asset, IAssetStorage storage, Object source)
        {
            if (asset != null)
            {
                asset.SetStorage(storage, source);
            }
        }

        private static Asset GetRoot(Object target)
        {
            var directable = target as DirectableAsset;
            while (directable != null && directable.parent != null)
            {
                directable = directable.parent;
            }

            return directable as Asset;
        }
    }
}
