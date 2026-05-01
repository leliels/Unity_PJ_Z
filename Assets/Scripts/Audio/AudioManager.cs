using System.Collections;
using System.Collections.Generic;
using BlockPuzzle.Core;
using BlockPuzzle.Save;
using UnityEngine;

namespace BlockPuzzle.Audio
{
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioLibrary _audioLibrary;
        [SerializeField] private int _sfxSourceCount = 8;

        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
        private AudioSource _bgmSource;
        private UserSettingsData _settings;
        private SaveManager _saveManager;

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this)
            {
                DontDestroyOnLoad(gameObject);
                EnsureLibrary();
                EnsureSources();
                LoadSettings();
            }
        }

        private void OnEnable()
        {
            _saveManager = SaveManager.Instance;
            if (_saveManager != null)
                _saveManager.OnSettingsChanged += HandleSettingsChanged;
        }

        private void OnDisable()
        {
            if (_saveManager != null)
                _saveManager.OnSettingsChanged -= HandleSettingsChanged;
            _saveManager = null;
        }

        public void PlayCue(AudioCueId cueId, float volumeMultiplier = 1f, float delayOverride = -1f, float pitchMultiplier = 1f)
        {
            EnsureLibrary();
            PlayCue(_audioLibrary != null ? _audioLibrary.GetCue(cueId) : null, volumeMultiplier, delayOverride, pitchMultiplier);
        }

        public void PlayCue(AudioCue cue, float volumeMultiplier = 1f, float delayOverride = -1f, float pitchMultiplier = 1f)
        {
            if (cue == null || !cue.CanPlay) return;
            LoadSettings();
            if (cue.UseGlobalSfxVolume && (_settings == null || !_settings.soundEnabled)) return;

            AudioClip clip = cue.GetClip();
            if (clip == null) return;

            cue.MarkPlayed();
            float delay = delayOverride >= 0f ? delayOverride : cue.Delay;
            if (delay > 0f)
                StartCoroutine(PlayCueDelayed(cue, clip, volumeMultiplier, pitchMultiplier, delay));
            else
                PlayClipNow(cue, clip, volumeMultiplier, pitchMultiplier);
        }

        public void PlayTitleBgm()
        {
            EnsureLibrary();
            PlayBgm(_audioLibrary != null ? _audioLibrary.TitleBgm : null, true);
        }

        public void PlayGameBgm()
        {
            EnsureLibrary();
            PlayBgm(_audioLibrary != null ? _audioLibrary.GameBgm : null, true);
        }

        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            EnsureSources();
            LoadSettings();
            bool shouldRestart = _bgmSource.clip != clip || !_bgmSource.isPlaying;
            _bgmSource.clip = clip;
            _bgmSource.loop = loop;
            _bgmSource.volume = (_settings?.musicEnabled ?? true) ? (_settings?.musicVolume ?? 1f) : 0f;
            if (shouldRestart)
                _bgmSource.Play();
        }

        public void StopBgm()
        {
            if (_bgmSource != null)
                _bgmSource.Stop();
        }

        private IEnumerator PlayCueDelayed(AudioCue cue, AudioClip clip, float volumeMultiplier, float pitchMultiplier, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            PlayClipNow(cue, clip, volumeMultiplier, pitchMultiplier);
        }

        private void PlayClipNow(AudioCue cue, AudioClip clip, float volumeMultiplier, float pitchMultiplier)
        {
            var source = GetFreeSfxSource();
            if (source == null) return;

            LoadSettings();
            source.pitch = cue.GetPitch() * pitchMultiplier;
            source.volume = cue.Volume * volumeMultiplier * (cue.UseGlobalSfxVolume ? (_settings?.soundVolume ?? 1f) : 1f);
            source.loop = cue.Loop;

            if (cue.Loop)
            {
                source.clip = clip;
                source.Play();
            }
            else
            {
                source.PlayOneShot(clip, source.volume);
            }
        }

        private AudioSource GetFreeSfxSource()
        {
            EnsureSources();
            foreach (var source in _sfxSources)
            {
                if (source != null && !source.isPlaying)
                    return source;
            }
            return _sfxSources.Count > 0 ? _sfxSources[0] : null;
        }

        private void EnsureLibrary()
        {
            if (_audioLibrary == null)
                _audioLibrary = Resources.Load<AudioLibrary>(AudioLibrary.ResourcesPath);
        }

        private void EnsureSources()
        {
            if (_bgmSource == null)
            {
                var bgmGo = new GameObject("BGMSource");
                bgmGo.transform.SetParent(transform, false);
                _bgmSource = bgmGo.AddComponent<AudioSource>();
                _bgmSource.playOnAwake = false;
            }

            while (_sfxSources.Count < Mathf.Max(1, _sfxSourceCount))
            {
                var go = new GameObject($"SFXSource_{_sfxSources.Count}");
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _sfxSources.Add(source);
            }
        }

        private void LoadSettings()
        {
            if (_saveManager == null)
                _saveManager = SaveManager.Current ?? SaveManager.Instance;
            _settings = _saveManager != null ? _saveManager.GetSettings() : new UserSettingsData();
            ApplySettings();
        }

        private void HandleSettingsChanged(UserSettingsData settings)
        {
            _settings = settings;
            ApplySettings();
        }

        private void ApplySettings()
        {
            if (_bgmSource != null && _settings != null)
                _bgmSource.volume = _settings.musicEnabled ? _settings.musicVolume : 0f;
        }
    }
}
