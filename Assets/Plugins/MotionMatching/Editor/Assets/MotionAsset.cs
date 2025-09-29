using System;
using System.Collections.Generic;
using TrackEditor;
using UnityEditor;
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

        [SerializeField]
        public Avatar destinationAvatar;

        [Range(0, 10), Header("Trajectory Similarity")]
        public int numPoses;

        [Range(0, 1)]
        public float poseSampleRatio;

        //TODO œ» ÷–¥∞…
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

        internal class DoCreateMotionAsset : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var asset = ScriptableObject.CreateInstance<MotionAsset>();
                AssetDatabase.CreateAsset(asset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(asset);
            }
        }

        [MenuItem("Assets/Create/MotionAsset", false, 30)]
        private static void CreateAsset()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreateMotionAsset>(),
                "New MotionAssets.asset",
                null,
                null
            );
        }
    }
}
