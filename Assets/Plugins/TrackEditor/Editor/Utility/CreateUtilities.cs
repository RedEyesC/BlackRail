using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    internal static class CreateUtilities
    {
        
        public static void SaveAssetIntoObject(Object childAsset, Object masterAsset)
        {
            Asset.SaveAssetIntoObject(childAsset, masterAsset);
        }
    }
}
