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

        // 标记本次放置是否触发了消除（用于 Combo 判定）
        private bool _clearedThisTurn;

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

            _clearedThisTurn = false;

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
            // 重置标记：等待消除事件
            _clearedThisTurn = false;

            // 放置方块加分（每格 1 分）
            ScoreManager.Instance.AddPlacementScore(cellCount);
        }

        private void HandleLinesCleared(int lineCount)
        {
            if (lineCount > 0)
            {
                _clearedThisTurn = true;
                // 消除加分（含 Combo 加成，由 ScoreManager 内部处理）
                ScoreManager.Instance.AddLineClearScore(lineCount);
            }
        }

        /// <summary>
        /// 在方块放置并完成消除检测后调用，用于处理“未消除是否重置 Combo”的配置开关。
        /// </summary>
        public void OnTurnComplete()
        {
            if (!_clearedThisTurn)
                ScoreManager.Instance.OnTurnCompletedWithoutClear();
        }


        private void HandleGameOver()
        {
            SetState(GameState.GameOver);
            // 游戏结束时尝试更新最高分
            ScoreManager.Instance.TryUpdateHighScore();
        }

        protected override void OnDestroy()
        {
            UnregisterEvents();
            base.OnDestroy();
        }
    }
}
