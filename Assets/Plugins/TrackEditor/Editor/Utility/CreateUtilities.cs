using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    internal static class CreateUtilities
    {
        
        public static void SaveAssetIntoObject(Object childAsset, Object masterAsset, IAssetStorage storage)
        {
            Asset.SaveAssetIntoObject(childAsset, masterAsset, storage);
        }
    }
}
