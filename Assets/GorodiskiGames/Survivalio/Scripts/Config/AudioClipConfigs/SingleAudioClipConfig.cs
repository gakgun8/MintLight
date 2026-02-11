using UnityEngine;
using System;

namespace Game.Config
{
    [Serializable]
    [CreateAssetMenu(menuName = "Config/SingleAudioClipConfig")]
    public class SingleAudioClipConfig : AudioClipConfig
    {
        public float StartTime;
        [Tooltip("Defines when (in seconds) the music clip should loop or switch to the next one. Ignored for SFX")]
        public float LoopTime;
    }
}

