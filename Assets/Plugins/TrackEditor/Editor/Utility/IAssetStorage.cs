using System;
using Object = UnityEngine.Object;

namespace TrackEditor
{
    public interface IAssetStorage
    {
        Asset Load(Object source, Type assetType);
        void Save(Asset asset, Object source);
    }
}
