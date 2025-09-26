using System.Collections.Generic;
using UnityEngine;

namespace TrackEditor
{
    public interface IDirector : IData
    {
        float Length { get; }

        // void Validate();

        void SaveToAssets();
    }
}
