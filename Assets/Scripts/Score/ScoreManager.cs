using System;
using BlockPuzzle.Core;
using BlockPuzzle.Utils;

namespace BlockPuzzle.Score
{
    /// <summary>
    /// 分数管理器
    /// </summary>
    public class ScoreManager : Singleton<ScoreManager>
    {
        /// <summary>分数变化事件（参数：当前总分）</summary>
        public event Action<int> OnScoreChanged;

        private int _score;
        public int CurrentScore => _score;

        /// <summary>
        /// 初始化/重置分数
        /// </summary>
        public void ResetScore()
        {
            _score = 0;
            OnScoreChanged?.Invoke(_score);
        }

        /// <summary>
        /// 放置方块加分
        /// </summary>
        public void AddPlacementScore(int cellCount)
        {
            _score += cellCount * Constants.ScorePerCell;
            OnScoreChanged?.Invoke(_score);
        }

        /// <summary>
        /// 消除行/列加分
        /// </summary>
        public void AddLineClearScore(int lineCount)
        {
            _score += Constants.GetLineClearScore(lineCount);
            OnScoreChanged?.Invoke(_score);
        }
    }
}
