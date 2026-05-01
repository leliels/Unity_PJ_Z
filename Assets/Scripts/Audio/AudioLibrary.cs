using UnityEngine;

namespace BlockPuzzle.Audio
{
    public enum AudioCueId
    {
        UiClick,
        UiOpen,
        UiClose,
        BlockPick,
        BlockDragBegin,
        BlockDropSuccess,
        BlockDropFailed,
        BlockPlace,
        ClearLine,
        ScoreTick,
        GameOver,
        CandidatesRefresh
    }

    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "BlockPuzzle/Audio/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        public const string ResourcesPath = "Configs/AudioLibrary";

        [SerializeField] private AudioCue _uiClick;
        [SerializeField] private AudioCue _uiOpen;
        [SerializeField] private AudioCue _uiClose;
        [SerializeField] private AudioCue _blockPick;
        [SerializeField] private AudioCue _blockDragBegin;
        [SerializeField] private AudioCue _blockDropSuccess;
        [SerializeField] private AudioCue _blockDropFailed;
        [SerializeField] private AudioCue _blockPlace;
        [SerializeField] private AudioCue _clearLine;
        [SerializeField] private AudioCue _scoreTick;
        [SerializeField] private AudioCue _gameOver;
        [SerializeField] private AudioCue _candidatesRefresh;
        [SerializeField] private AudioClip _titleBgm;
        [SerializeField] private AudioClip _gameBgm;

        public AudioClip TitleBgm => _titleBgm;
        public AudioClip GameBgm => _gameBgm;

        public AudioCue GetCue(AudioCueId id)
        {
            return id switch
            {
                AudioCueId.UiClick => _uiClick,
                AudioCueId.UiOpen => _uiOpen,
                AudioCueId.UiClose => _uiClose,
                AudioCueId.BlockPick => _blockPick,
                AudioCueId.BlockDragBegin => _blockDragBegin,
                AudioCueId.BlockDropSuccess => _blockDropSuccess,
                AudioCueId.BlockDropFailed => _blockDropFailed,
                AudioCueId.BlockPlace => _blockPlace,
                AudioCueId.ClearLine => _clearLine,
                AudioCueId.ScoreTick => _scoreTick,
                AudioCueId.GameOver => _gameOver,
                AudioCueId.CandidatesRefresh => _candidatesRefresh,
                _ => null
            };
        }

#if UNITY_EDITOR
        public void EditorSetAll(AudioCue cue)
        {
            _uiClick = cue;
            _uiOpen = cue;
            _uiClose = cue;
            _blockPick = cue;
            _blockDragBegin = cue;
            _blockDropSuccess = cue;
            _blockDropFailed = cue;
            _blockPlace = cue;
            _clearLine = cue;
            _scoreTick = cue;
            _gameOver = cue;
            _candidatesRefresh = cue;
        }

        public void EditorSetBgm(AudioClip titleBgm, AudioClip gameBgm)
        {
            _titleBgm = titleBgm;
            _gameBgm = gameBgm;
        }
#endif
    }
}
