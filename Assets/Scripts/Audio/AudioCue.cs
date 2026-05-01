using UnityEngine;

namespace BlockPuzzle.Audio
{
    [CreateAssetMenu(fileName = "AudioCue", menuName = "BlockPuzzle/Audio/Audio Cue")]
    public class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clips;
        [Range(0f, 1f)] [SerializeField] private float _volume = 1f;
        [Min(0f)] [SerializeField] private float _delay;
        [SerializeField] private Vector2 _pitchRange = Vector2.one;
        [SerializeField] private bool _loop;
        [Min(0f)] [SerializeField] private float _cooldown;
        [SerializeField] private bool _useGlobalSfxVolume = true;

        private float _lastPlayedTime = -999f;

        public AudioClip[] Clips => _clips;
        public float Volume => _volume;
        public float Delay => _delay;
        public Vector2 PitchRange => _pitchRange;
        public bool Loop => _loop;
        public float Cooldown => _cooldown;
        public bool UseGlobalSfxVolume => _useGlobalSfxVolume;

        public bool CanPlay => Time.unscaledTime - _lastPlayedTime >= _cooldown;

        public AudioClip GetClip()
        {
            if (_clips == null || _clips.Length == 0) return null;
            if (_clips.Length == 1) return _clips[0];
            return _clips[Random.Range(0, _clips.Length)];
        }

        public float GetPitch()
        {
            float min = Mathf.Min(_pitchRange.x, _pitchRange.y);
            float max = Mathf.Max(_pitchRange.x, _pitchRange.y);
            if (Mathf.Approximately(min, max)) return min;
            return Random.Range(min, max);
        }

        public void MarkPlayed()
        {
            _lastPlayedTime = Time.unscaledTime;
        }

#if UNITY_EDITOR
        public void EditorSetValues(AudioClip[] clips, float volume = 1f, float delay = 0f, Vector2? pitchRange = null, bool loop = false, float cooldown = 0f, bool useGlobalSfxVolume = true)
        {
            _clips = clips;
            _volume = volume;
            _delay = delay;
            _pitchRange = pitchRange ?? Vector2.one;
            _loop = loop;
            _cooldown = cooldown;
            _useGlobalSfxVolume = useGlobalSfxVolume;
        }
#endif
    }
}
