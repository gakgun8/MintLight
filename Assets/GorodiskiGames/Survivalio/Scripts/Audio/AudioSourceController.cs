using Game.Config;
using UnityEngine;

namespace Game.Audio
{
    public abstract class AudioSourceController
    {
        protected readonly AudioSourceView _view;

        public AudioSourceView View => _view;

        public AudioSourceController(AudioSourceView view, AudioClip clip, float startTime, float volumeResult)
        {
            _view = view;

            var source = _view.Source;
            source.playOnAwake = false;
            source.clip = clip;
            source.time = startTime;
            source.loop = false;
            source.volume = volumeResult;
        }

        public void Play()
        {
            _view.Source.Play();
        }
    }

    public sealed class SFXSourceController : AudioSourceController
    {
        private SFXType _type;

        public SFXType Type => _type;

        public SFXSourceController(AudioSourceView view, AudioClip clip, float startTime, float volumeResult, SFXType type)
            : base(view, clip, startTime, volumeResult)
        {
            _type = type;
        }
    }

    public sealed class MusicSourceController : AudioSourceController
    {
        private MusicType _type;
        private float _clipVolume;
        private float _fadeStartTime;

        public float ClipVolume => _clipVolume;
        public float FadeStartTime => _fadeStartTime;
        public MusicType Type => _type;

        public MusicSourceController(AudioSourceView view, AudioClip clip, float startTime, float volumeResult, MusicType type)
            : base(view, clip, startTime, volumeResult)
        {
            _type = type;
        }

        public MusicSourceController(AudioSourceView view, AudioClip clip, float startTime, float volumeResult, MusicType type, float clipVolume, float fadeStartTime)
            : base(view, clip, startTime, volumeResult)
        {
            _type = type;
            _clipVolume = clipVolume;
            _fadeStartTime = fadeStartTime;
        }
    }
}

