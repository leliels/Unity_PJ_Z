using System;
using UnityEngine;
using BlockPuzzle.Board;
using BlockPuzzle.Block;
using BlockPuzzle.Score;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// 游戏总管理器：协调各系统初始化和状态控制
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        /// <summary>游戏状态变化事件</summary>
        public event Action<GameState> OnGameStateChanged;

        private GameState _currentState;
        public GameState CurrentState => _currentState;

        private void Start()
        {
            StartGame();
        }

        /// <summary>
        /// 开始新游戏
        /// </summary>
        public void StartGame()
        {
            // 初始化分数
            ScoreManager.Instance.ResetScore();

            // 初始化棋盘
            BoardManager.Instance.ClearBoard();

            // 初始化候选方块
            BlockSpawner.Instance.ClearAll();
            BlockSpawner.Instance.Init();

            // 注册事件
            RegisterEvents();

            // 设置状态
            SetState(GameState.Playing);
        }

        /// <summary>
        /// 重新开始游戏
        /// </summary>
        public void RestartGame()
        {
            UnregisterEvents();
            StartGame();
        }

        private void SetState(GameState newState)
        {
            _currentState = newState;
            OnGameStateChanged?.Invoke(_currentState);
        }

        // ==================== 事件管理 ====================

        private void RegisterEvents()
        {
            BoardManager.Instance.OnBlockPlaced += HandleBlockPlaced;
            BoardManager.Instance.OnLinesCleared += HandleLinesCleared;
            BoardManager.Instance.OnGameOver += HandleGameOver;
        }

        private void UnregisterEvents()
        {
            if (BoardManager.Instance != null)
            {
                BoardManager.Instance.OnBlockPlaced -= HandleBlockPlaced;
                BoardManager.Instance.OnLinesCleared -= HandleLinesCleared;
                BoardManager.Instance.OnGameOver -= HandleGameOver;
            }
        }

        private void HandleBlockPlaced(int cellCount)
        {
            ScoreManager.Instance.AddPlacementScore(cellCount);
        }

        private void HandleLinesCleared(int lineCount)
        {
            ScoreManager.Instance.AddLineClearScore(lineCount);
        }

        private void HandleGameOver()
        {
            SetState(GameState.GameOver);
        }

        protected override void OnDestroy()
        {
            UnregisterEvents();
            base.OnDestroy();
        }
    }
}
