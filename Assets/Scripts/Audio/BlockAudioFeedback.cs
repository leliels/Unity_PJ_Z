using UnityEngine;

namespace BlockPuzzle.Audio
{
    public class BlockAudioFeedback : MonoBehaviour
    {
        [SerializeField] private AudioCue _pickCue;
        [SerializeField] private AudioCue _dragBeginCue;
        [SerializeField] private AudioCue _dropSuccessCue;
        [SerializeField] private AudioCue _dropFailedCue;
        [SerializeField] private AudioCue _cancelCue;
        [SerializeField] private bool _useLibraryFallback = true;

        public void PlayPick() => Play(_pickCue, AudioCueId.BlockPick);
        public void PlayDragBegin() => Play(_dragBeginCue, AudioCueId.BlockDragBegin);
        public void PlayDropSuccess() => Play(_dropSuccessCue, AudioCueId.BlockDropSuccess);
        public void PlayDropFailed() => Play(_dropFailedCue, AudioCueId.BlockDropFailed);
        public void PlayCancel() => Play(_cancelCue, AudioCueId.BlockDropFailed);

        private void Play(AudioCue cue, AudioCueId fallbackId)
        {
            var audioManager = AudioManager.Current;
            if (audioManager == null) return;
            if (cue != null)
                audioManager.PlayCue(cue);
            else if (_useLibraryFallback)
                audioManager.PlayCue(fallbackId);
        }
    }
}
