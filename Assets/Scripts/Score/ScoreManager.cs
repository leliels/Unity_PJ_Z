using System;
using BlockPuzzle.Core;
using BlockPuzzle.Utils;
using UnityEngine;

namespace BlockPuzzle.Score
{
    /// <summary>
    /// 分数管理器（2026-04-23 新版：16^N 消除分 + Combo 连击）
    /// </summary>
    public class ScoreManager : Singleton<ScoreManager>
    {
        /// <summary>分数变化事件（参数：当前总分）</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>Combo 变化事件（参数：当前 Combo 次数）</summary>
        public event Action<int> OnComboChanged;

        /// <summary>消除计分详情事件（参数：消除行数、基础分、Combo 加成分、Combo 次数）</summary>
        public event Action<int, long, long, int> OnLineClearScoreDetail;

        private int _score;
        public int CurrentScore => _score;

        private int _comboCount;
        /// <summary>当前 Combo 次数（0 = 无 Combo）</summary>
        public int ComboCount => _comboCount;

        /// <summary>
        /// 初始化/重置分数和 Combo
        /// </summary>
        public void ResetScore()
        {
            _score = 0;
            _comboCount = 0;
            OnScoreChanged?.Invoke(_score);
            OnComboChanged?.Invoke(_comboCount);
        }

        /// <summary>
        /// 放置方块加分（每格 1 分）
        /// </summary>
        public void AddPlacementScore(int cellCount)
        {
            _score += cellCount * Constants.ScorePerCell;
            OnScoreChanged?.Invoke(_score);
        }

        /// <summary>
        /// 消除行/列加分（16^N 幂次 × Combo 系数）
        /// </summary>
        public void AddLineClearScore(int lineCount)
        {
            if (lineCount <= 0)
            {
                ResetCombo();
                return;
            }

            // 计算消除基础分 = 16^N
            long baseScore = Constants.GetLineClearScore(lineCount);

            // 应用 Combo 系数
            float comboMultiplier = Constants.GetComboMultiplier(_comboCount);
            long comboBonus = (long)Mathf.RoundToInt(baseScore * (comboMultiplier - 1f));
            long finalScore = baseScore + comboBonus;

            _score += (int)Mathf.Min(finalScore, int.MaxValue);

            Debug.Log($"[Score] 消除 {lineCount} 排 | 基础分 {baseScore} | Combo ×{_comboCount} (×{comboMultiplier:F1}) 加成 {comboBonus} | 得分 {finalScore} | 总分 {_score}");

            // 发出详细事件供飘字系统使用
            OnLineClearScoreDetail?.Invoke(lineCount, baseScore, comboBonus, _comboCount);

            // 消除成功 → Combo +1
            _comboCount++;
            OnComboChanged?.Invoke(_comboCount);

            // 最后更新总分显示
            OnScoreChanged?.Invoke(_score);
        }

        /// <summary>
        /// 放置方块后未消除时调用，重置 Combo
        /// </summary>
        public void ResetCombo()
        {
            if (_comboCount > 0)
            {
                _comboCount = 0;
                OnComboChanged?.Invoke(_comboCount);
            }
        }
    }
}
