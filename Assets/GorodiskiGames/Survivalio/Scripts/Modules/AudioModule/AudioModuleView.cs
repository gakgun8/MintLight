using UnityEngine;
using Game.UI.Pool;
using Game.Config;
using System.Collections.Generic;

namespace Game.Modules
{
    public class AudioModuleView : MonoBehaviour
    {
        [Header("Audio Sources")]
        public ComponentPoolFactory AudioSourcesPool;

        [Header("Music")]
        [SerializeField] private SingleAudioClipConfig[] _gamePlayMusicConfigs;
        [SerializeField] private SingleAudioClipConfig[] _menuMusicConfigs;

        [Header("SFX")]
        [SerializeField] private AudioClipConfig[] _footstepConfig;
        [SerializeField] private AudioClipConfig[] _explosionConfig;

        public Dictionary<MusicType, SingleAudioClipConfig[]> MusicMap;
        public Dictionary<SFXType, AudioClipConfig[]> SFXMap;

        private void Awake()
        {
            MusicMap = new Dictionary<MusicType, SingleAudioClipConfig[]>();
            MusicMap[MusicType.GamePlay] = _gamePlayMusicConfigs;
            MusicMap[MusicType.Menu] = _menuMusicConfigs;

            SFXMap = new Dictionary<SFXType, AudioClipConfig[]>();
            SFXMap[SFXType.Footsteps] = _footstepConfig;
            SFXMap[SFXType.Explosion] = _explosionConfig;
        }
    }
}

