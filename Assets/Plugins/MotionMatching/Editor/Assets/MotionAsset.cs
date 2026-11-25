using System;
using System.Collections.Generic;
using TrackEditor;
using UnityEngine;

namespace MotionMatching
{
    [Serializable]
    public class MotionAsset : Asset
    {
        [Range(0.0f, 5.0f), Header("Base Settings")]
        public float timeHorizon;

        [Range(0.0f, 120.0f)]
        public float sampleRate;

        [TimelineAssetReference(typeof(Avatar))]
        public TimelineAssetReference destinationAvatar;

        [Range(0, 10), Header("Trajectory Similarity")]
        public int numPoses;

        [Range(0, 1)]
        public float poseSampleRatio;

        //TODO 先手写吧
        [SerializeField]
        public List<string> joints;

        [Range(0, 1)]
        public float trajectorySampleRatio;

        [Range(-1, 1)]
        public float trajectorySampleRange;

        [SerializeField]
        public bool trajectoryDisplacements;

        [SerializeField, Header("Product Quantization Settings")]
        public int numAttempts;

        [SerializeField]
        public int numIterations;

        [SerializeField]
        public int minimumNumberSamples;

        [SerializeField]
        public int maximumNumberSamples;
    }
}
