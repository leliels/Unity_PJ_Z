using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using BlockPuzzle.Board;
using BlockPuzzle.Block;
using BlockPuzzle.Mode;
using BlockPuzzle.Save;
using BlockPuzzle.Score;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// 游戏总管理器：协调传统模式的游戏流程和状态控制
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        /// <summary>游戏状态变化事件</summary>
        public event Action<GameState> OnGameStateChanged;

        private GameState _currentState;
        public GameState CurrentState => _currentState;

        private bool _clearedThisTurn;
        private bool _eventsRegistered;
        private bool _resultRecorded;
        private DateTime _playStartTime;
        private BoardManager _boardManager;

        private void Start()
        {
            StartGame();
        }

        /// <summary>开始新游戏</summary>
        public void StartGame()
        {
            _resultRecorded = false;
            _playStartTime = DateTime.Now;

            ScoreManager.Instance.ResetScore();
            BoardManager.Instance.ClearBoard();
            BlockSpawner.Instance.ClearAll();
            BlockSpawner.Instance.Init();

            _clearedThisTurn = false;
            RegisterEvents();
            SetState(GameState.Playing);
        }

        /// <summary>重新开始游戏</summary>
        public void RestartGame()
        {
            UnregisterEvents();
            StartGame();
        }

        public void PauseGame()
        {
            if (_currentState == GameState.Playing)
                SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (_currentState == GameState.Paused)
                SetState(GameState.Playing);
        }

        public void ReturnToTitle()
        {
            RecordResultIfNeeded(false);
            UnregisterEvents();
            if (ModeManager.Instance != null)
                ModeManager.Instance.ReturnToTitle();
            else
                SceneManager.LoadScene("Title");
        }

        private void SetState(GameState newState)
        {
            _currentState = newState;
            OnGameStateChanged?.Invoke(_currentState);
        }

        private void RegisterEvents()
        {
            if (_eventsRegistered) return;
            _boardManager = BoardManager.Current ?? BoardManager.Instance;
            if (_boardManager == null) return;

            _boardManager.OnBlockPlaced += HandleBlockPlaced;
            _boardManager.OnLinesCleared += HandleLinesCleared;
            _boardManager.OnGameOver += HandleGameOver;
            _eventsRegistered = true;
        }

        private void UnregisterEvents()
        {
            if (!_eventsRegistered || _boardManager == null) return;
            _boardManager.OnBlockPlaced -= HandleBlockPlaced;
            _boardManager.OnLinesCleared -= HandleLinesCleared;
            _boardManager.OnGameOver -= HandleGameOver;
            _boardManager = null;
            _eventsRegistered = false;
        }

        private void HandleBlockPlaced(int cellCount)
        {
            _clearedThisTurn = false;
            ScoreManager.Instance.RecordPlacedCells(cellCount);
        }

        private void HandleLinesCleared(int lineCount)
        {
            if (lineCount <= 0) return;
            _clearedThisTurn = true;
            ScoreManager.Instance.AddLineClearScore(lineCount);
        }

        /// <summary>在方块放置并完成消除检测后调用，用于处理未消除时的 CCD 递减。</summary>
        public void OnTurnComplete()
        {
            if (!_clearedThisTurn)
                ScoreManager.Instance.OnTurnCompletedWithoutClear();
        }

        private void HandleGameOver()
        {
            ScoreManager.Instance.TryUpdateHighScore();
            RecordResultIfNeeded(true);
            SetState(GameState.GameOver);
        }

        private void RecordResultIfNeeded(bool gameOver)
        {
            if (_resultRecorded || SaveManager.Instance == null || ScoreManager.Instance == null) return;
            if (!gameOver && ScoreManager.Instance.CurrentScore <= 0) return;

            string modeId = ModeManager.Instance != null ? ModeManager.Instance.CurrentModeId : GameModeConfig.TraditionalId;
            SaveManager.Instance.RegisterPlayResult(modeId, ScoreManager.Instance.CurrentScore, _playStartTime, DateTime.Now);
            _resultRecorded = true;
        }

        protected override void OnDestroy()
        {
            UnregisterEvents();
            base.OnDestroy();
        }
    }
}

