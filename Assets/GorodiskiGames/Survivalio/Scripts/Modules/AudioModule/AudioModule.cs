using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Audio;
using Game.Config;
using Game.Core;
using Game.Managers;
using Injection;
using UnityEngine;

namespace Game.Modules
{
    public sealed class AudioModule : Module<AudioModuleView>
    {
        private const float _changeTypeFadeDuration = 1.5f;
        private const float _crossFadeDuration = 5f;
        private const float _volumeChangeFade = 0.5f;

        [Inject] private AudioManager _audioManager;
        [Inject] private Timer _timer;

        private MusicSourceController _musicSource;

        private readonly Dictionary<SFXSourceController, float> _sfxMap;
        private readonly Dictionary<MusicSourceController, Coroutine> _coroutinesMap;
        private readonly Dictionary<MusicType, int> _musicIndexesMap;

        public AudioModule(AudioModuleView view) : base(view)
        {
            _sfxMap = new Dictionary<SFXSourceController, float>();
            _coroutinesMap = new Dictionary<MusicSourceController, Coroutine>();
            _musicIndexesMap = new Dictionary<MusicType, int>();
        }

        public override void Initialize()
        {
            _musicIndexesMap[MusicType.GamePlay] = 0;
            _musicIndexesMap[MusicType.Menu] = 0;

            _audioManager.ON_PLAY_MUSIC += TryPlayMusic;
            _audioManager.ON_PLAY_SFX += OnPlaySFX;
            _audioManager.ON_MUSIC_VOLUME_CHANGED += OnMusicVolumeChanged;
            _timer.TICK += OnTick;
        }

        public override void Dispose()
        {
            _audioManager.ON_PLAY_MUSIC -= TryPlayMusic;
            _audioManager.ON_PLAY_SFX -= OnPlaySFX;
            _audioManager.ON_MUSIC_VOLUME_CHANGED -= OnMusicVolumeChanged;
            _timer.TICK -= OnTick;
        }

        private void OnTick()
        {
            foreach (var sfx in _sfxMap.Keys.ToList())
            {
                var duration = _sfxMap[sfx];
                duration -= Time.deltaTime;

                _sfxMap[sfx] = duration;

                if (duration > 0f)
                    continue;

                _sfxMap.Remove(sfx);
                _view.AudioSourcesPool.Release(sfx.View);
            }

            if (_musicSource == null)
                return;

            var time = _musicSource.View.Source.time;
            var fadeStartTime = _musicSource.FadeStartTime;
            if (time < fadeStartTime)
                return;

            PlayNext(_crossFadeDuration);
        }

        private void TryPlayMusic(MusicType type)
        {
            if (_musicSource == null)
            {
                PlayMusic(type, _musicIndexesMap[type]);
                FadeToTargetVolume(_musicSource, _audioManager.MusicVolume, _changeTypeFadeDuration, false);
                return;
            }
            
            if (_musicSource.Type == type)
                return;

            FadeToTargetVolume(_musicSource, 0f, _changeTypeFadeDuration, true);
            PlayMusic(type, _musicIndexesMap[type]);
            FadeToTargetVolume(_musicSource, _audioManager.MusicVolume, _changeTypeFadeDuration, false);
        }

        public void PlayNext(float fadeDuration)
        {
            var type = _musicSource.Type;
            var configs = _view.MusicMap[type];
            var index = _musicIndexesMap[type] + 1;

            if (index >= configs.Length)
                index = 0;

            _musicIndexesMap[type] = index;

            FadeToTargetVolume(_musicSource, 0f, fadeDuration, true);
            PlayMusic(type, index);
            FadeToTargetVolume(_musicSource, _audioManager.MusicVolume, fadeDuration, false);
        }

        private void OnMusicVolumeChanged(float newVolume)
        {
            FadeToTargetVolume(_musicSource, newVolume, _volumeChangeFade, false);
        }

        private void FadeToTargetVolume(MusicSourceController audioSource, float targetVolume, float duration, bool isRelease)
        {
            var startVolume = audioSource.View.Source.volume;
            var clipVolume = audioSource.ClipVolume;
            var volumeResult = targetVolume * clipVolume;

            if (_coroutinesMap.TryGetValue(audioSource, out Coroutine existed))
                _view.StopCoroutine(existed);

            var coroutine = _view.StartCoroutine(FadeToVolume(audioSource, startVolume, volumeResult, duration, isRelease));
            _coroutinesMap[audioSource] = coroutine;
        }

        public void PlayMusic(MusicType type, int index)
        {
            var configs = _view.MusicMap[type];
            var config = configs[index];
            var startTime = config.StartTime;
            var loopTime = (config.LoopTime > 0) ? config.LoopTime : config.Clip.length;
            var fadeStartTime = loopTime - _crossFadeDuration;
            var clipVolume = config.Volume;
            var clip = config.Clip;
            var volumeResult = 0f;

            var view = _view.AudioSourcesPool.Get<AudioSourceView>();
            var audioSource = new MusicSourceController(view, clip, startTime, volumeResult, type, clipVolume, fadeStartTime);
            audioSource.Play();

            _musicSource = audioSource;
        }

        private IEnumerator FadeToVolume(MusicSourceController audioSource, float startVolume, float targetVolume, float duration, bool isRelease)
        {
            var time = 0f;
            var source = audioSource.View.Source;

            while (time < duration)
            {
                time += Time.deltaTime;
                source.volume = Mathf.Clamp01(Mathf.Lerp(startVolume, targetVolume, time / duration));
                yield return null;
            }

            source.volume = Mathf.Clamp01(targetVolume);
            _coroutinesMap.Remove(audioSource);

            if (!isRelease)
                yield break;

            _view.AudioSourcesPool.Release(audioSource.View);
        }

        private void OnPlaySFX(SFXType type)
        {
            var view = _view.AudioSourcesPool.Get<AudioSourceView>();

            var configs = _view.SFXMap[type];
            var config = configs[Random.Range(0, configs.Length)];
            var clip = config.Clip;

            var startTime = 0f;
            var duration = 0f;

            if (config is SingleAudioClipConfig single)
            {
                startTime = single.StartTime;
                var lenght = clip.length;
                duration = lenght - startTime;
            }
            else if (config is SegmentedAudioClipConfig segmented)
            {
                startTime = segmented.StartTimes[Random.Range(0, segmented.StartTimes.Length)];
                duration = segmented.SegmentDuration;
            }

            var clipVolume = config.Volume;
            var sfxVolume = _audioManager.SFXVolume;
            var volumeResult = sfxVolume * clipVolume;

            var source = new SFXSourceController(view, clip, startTime, volumeResult, type);
            source.Play();

            _sfxMap[source] = duration;
        }
    }
}
