using UnityEngine;

namespace TrackEditor
{
    public interface IAssetStorage
    {
        bool CanLoad(Object source);

        Asset Load(Object source);

        void Save(Asset asset, Object source);

        void SaveAssetIntoObject(Object childAsset, Object masterAsset);
    }
}
