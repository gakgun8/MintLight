using System;
using Game.Config;

namespace Game.Managers
{
    public sealed class AudioManager : IDisposable
    {
        public event Action<MusicType> ON_PLAY_MUSIC;
        public event Action<SFXType> ON_PLAY_SFX;
        public event Action<float> ON_MUSIC_VOLUME_CHANGED;

        public float MusicVolume;
        public float SFXVolume;

        public AudioManager(float musicVolume, float sfxVolume)
        {
            MusicVolume = musicVolume;
            SFXVolume = sfxVolume;
        }

        public void Dispose()
        {

        }

        public void FirePlayMusic(MusicType type)
        {
            ON_PLAY_MUSIC?.Invoke(type);
        }

        public void FirePlaySFX(SFXType type)
        {
            ON_PLAY_SFX?.Invoke(type);
        }

        public void FireMusicVolumeChanged(float value)
        {
            ON_MUSIC_VOLUME_CHANGED?.Invoke(value);
        }
    }
}

