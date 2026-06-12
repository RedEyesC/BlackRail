using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TrackEditor
{
    public sealed class AssetStorage : IAssetStorage
    {
        public static readonly IAssetStorage Default = new AssetStorage();

        public bool CanLoad(Object source)
        {
            return source is Asset;
        }

        public Asset Load(Object source)
        {
            return source as Asset;
        }

        public void Save(Asset asset, Object source)
        {
            if (asset == null)
            {
                return;
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        public void SaveAssetIntoObject(Object childAsset, Object masterAsset)
        {
            if (childAsset == null || masterAsset == null)
            {
                return;
            }

            if ((masterAsset.hideFlags & HideFlags.DontSave) != 0)
            {
                childAsset.hideFlags |= HideFlags.DontSave;
                return;
            }

            childAsset.hideFlags |= HideFlags.HideInHierarchy;
            if (!AssetDatabase.Contains(childAsset) && AssetDatabase.Contains(masterAsset))
            {
                AssetDatabase.AddObjectToAsset(childAsset, masterAsset);
            }
        }
    }
}
