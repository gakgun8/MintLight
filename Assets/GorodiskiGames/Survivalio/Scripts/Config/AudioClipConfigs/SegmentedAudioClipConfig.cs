using UnityEngine;
using System;

namespace Game.Config
{
    [Serializable]
    [CreateAssetMenu(menuName = "Config/SegmentedAudioClipConfig")]
    public class SegmentedAudioClipConfig : AudioClipConfig
    {
        [Tooltip("In seconds")]
        public float[] StartTimes;
        public float SegmentDuration = 0.2f;
    }
}

