using UnityEngine;

namespace BlockPuzzle.Audio
{
    public enum ObjectAudioTriggerEvent
    {
        OnEnable,
        OnDisable,
        OnDestroy,
        Manual
    }

    public class ObjectAudioTrigger : MonoBehaviour
    {
        [SerializeField] private ObjectAudioTriggerEvent _triggerEvent = ObjectAudioTriggerEvent.OnEnable;
        [SerializeField] private AudioCue _cue;
        [SerializeField] private AudioCueId _libraryCueId = AudioCueId.UiOpen;
        [SerializeField] private bool _useLibraryCue = true;
        [SerializeField] private float _volumeMultiplier = 1f;
        [SerializeField] private float _delayOverride = -1f;
        [SerializeField] private float _pitchMultiplier = 1f;

        private bool _isQuitting;

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnEnable()
        {
            if (_triggerEvent == ObjectAudioTriggerEvent.OnEnable)
                Play();
        }

        private void OnDisable()
        {
            if (!_isQuitting && _triggerEvent == ObjectAudioTriggerEvent.OnDisable)
                Play();
        }

        private void OnDestroy()
        {
            if (!_isQuitting && _triggerEvent == ObjectAudioTriggerEvent.OnDestroy)
                Play();
        }

        public void Play()
        {
            var audioManager = AudioManager.Current;
            if (audioManager == null) return;
            if (_useLibraryCue || _cue == null)
                audioManager.PlayCue(_libraryCueId, _volumeMultiplier, _delayOverride, _pitchMultiplier);
            else
                audioManager.PlayCue(_cue, _volumeMultiplier, _delayOverride, _pitchMultiplier);
        }
    }
}
