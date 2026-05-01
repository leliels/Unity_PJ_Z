using BlockPuzzle.Block;
using BlockPuzzle.Board;
using BlockPuzzle.Core;
using BlockPuzzle.Feedback;
using BlockPuzzle.Score;
using UnityEngine;

namespace BlockPuzzle.Audio
{
    public class GameplayAudioBinder : MonoBehaviour
    {
        [SerializeField] private AudioCue _blockPlaceCue;
        [SerializeField] private AudioCue _clearLineCue;
        [SerializeField] private AudioCue _scoreTickCue;
        [SerializeField] private AudioCue _gameOverCue;
        [SerializeField] private AudioCue _candidatesRefreshCue;
        [SerializeField] private bool _useLibraryFallback = true;

        private BoardManager _boardManager;
        private ScoreManager _scoreManager;
        private BlockSpawner _blockSpawner;

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            Unsubscribe();

            _boardManager = BoardManager.Current;
            if (_boardManager != null)
            {
                _boardManager.OnBlockPlaced += HandleBlockPlaced;
                _boardManager.OnLinesCleared += HandleLinesCleared;
                _boardManager.OnGameOver += HandleBoardGameOver;
            }

            _scoreManager = ScoreManager.Current;
            if (_scoreManager != null)
                _scoreManager.OnLineClearScoreDetail += HandleLineClearScoreDetail;

            _blockSpawner = BlockSpawner.Current;
            if (_blockSpawner != null)
                _blockSpawner.OnCandidatesRefreshed += HandleCandidatesRefreshed;
        }

        private void Unsubscribe()
        {
            if (_boardManager != null)
            {
                _boardManager.OnBlockPlaced -= HandleBlockPlaced;
                _boardManager.OnLinesCleared -= HandleLinesCleared;
                _boardManager.OnGameOver -= HandleBoardGameOver;
                _boardManager = null;
            }

            if (_scoreManager != null)
            {
                _scoreManager.OnLineClearScoreDetail -= HandleLineClearScoreDetail;
                _scoreManager = null;
            }

            if (_blockSpawner != null)
            {
                _blockSpawner.OnCandidatesRefreshed -= HandleCandidatesRefreshed;
                _blockSpawner = null;
            }
        }

        private void HandleBlockPlaced(int cellCount)
        {
            Play(_blockPlaceCue, AudioCueId.BlockPlace);
        }

        private void HandleLinesCleared(int lineCount)
        {
            Play(_clearLineCue, AudioCueId.ClearLine);
            if (FeedbackManager.Current != null)
                FeedbackManager.Current.PlayMedium();
        }

        private void HandleLineClearScoreDetail(int lineCount, long cellScore, long clearComboScore, int comboCount)
        {
            Play(_scoreTickCue, AudioCueId.ScoreTick);
        }

        private void HandleBoardGameOver()
        {
            Play(_gameOverCue, AudioCueId.GameOver);
            if (FeedbackManager.Current != null)
                FeedbackManager.Current.PlayLight();
        }

        private void HandleCandidatesRefreshed()
        {
            Play(_candidatesRefreshCue, AudioCueId.CandidatesRefresh);
        }

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
