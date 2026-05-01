using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Audio
{
    public enum UIAudioTriggerEvent
    {
        OnClick,
        OnEnable,
        OnDisable,
        Manual
    }

    public class UIAudioTrigger : MonoBehaviour
    {
        [SerializeField] private UIAudioTriggerEvent _triggerEvent = UIAudioTriggerEvent.OnClick;
        [SerializeField] private AudioCue _cue;
        [SerializeField] private AudioCueId _libraryCueId = AudioCueId.UiClick;
        [SerializeField] private bool _useLibraryCue = true;
        [SerializeField] private float _volumeMultiplier = 1f;
        [SerializeField] private float _delayOverride = -1f;
        [SerializeField] private float _pitchMultiplier = 1f;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_triggerEvent == UIAudioTriggerEvent.OnClick && _button != null)
                _button.onClick.AddListener(Play);
            else if (_triggerEvent == UIAudioTriggerEvent.OnEnable)
                Play();
        }

        private void OnDisable()
        {
            if (_triggerEvent == UIAudioTriggerEvent.OnClick && _button != null)
                _button.onClick.RemoveListener(Play);
            else if (_triggerEvent == UIAudioTriggerEvent.OnDisable)
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
