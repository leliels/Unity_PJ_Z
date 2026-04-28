using System;
using BlockPuzzle.Core;
using BlockPuzzle.Utils;
using UnityEngine;

namespace BlockPuzzle.Score
{
    /// <summary>
    /// 分数管理器（2026-04-28 新版：排分制 + Combo 连击）
    /// 公式：总分 += B*排分 + 排数*[20*Combo*(1+排数)]
    ///
    /// Combo 规则（设计文档 118-124）：
    /// - 每次消除后开启 3 次 Combo 窗口
    /// - 窗口内消除 → 有 Combo 加成，消耗 1 次窗口，Combo += 消除排数
    /// - 消除N排 = Combo+N（如消2排=2次combo）
    /// - 窗口用完(3次) → Combo归零，下次消除开启新窗口
    /// </summary>
    public class ScoreManager : Singleton<ScoreManager>
    {
        /// <summary>分数变化事件（参数：当前总分）</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>Combo 变化事件（参数：当前 Combo 次数）</summary>
        public event Action<int> OnComboChanged;

        /// <summary>消除计分详情事件（参数：消除行数、基础分、Combo 加成分、Combo 次数）</summary>
        public event Action<int, long, long, int> OnLineClearScoreDetail;

        /// <summary>最高分变化事件（参数：新的最高分）</summary>
        public event Action<int> OnHighScoreChanged;

        private int _score;
        public int CurrentScore => _score;

        private int _highScore;
        /// <summary>历史最高分（从 PlayerPrefs 加载）</summary>
        public int HighScore => _highScore;

        private int _comboCount;
        /// <summary>当前 Combo 累计次数（窗口内累加，每轮结束归零）</summary>
        public int ComboCount => _comboCount;

        /// <summary>Combo 窗口剩余次数（0=无窗口，1~3=剩余可触发 Combo 的消除次数）</summary>
        private int _comboWindowRemain;

        /// <summary>本次放置的方块得分（供消除公式使用）</summary>
        private int _lastPlacementScore;

        /// <summary>
        /// 初始化/重置分数和 Combo（不重置最高分）
        /// </summary>
        public void ResetScore()
        {
            _score = 0;
            _comboCount = 0;
            _comboWindowRemain = 0;
            // 首次或重置时从 PlayerPrefs 加载最高分
            if (_highScore == 0)
                _highScore = PlayerPrefs.GetInt("BlockPuzzle_HighScore", 0);
            OnScoreChanged?.Invoke(_score);
            OnComboChanged?.Invoke(_comboCount);
        }

        /// <summary>
        /// 尝试更新最高分（游戏结束时调用）。
        /// 如果当前分数超过历史最高分，则更新并保存。
        /// </summary>
        /// <returns>是否刷新了最高分</returns>
        public bool TryUpdateHighScore()
        {
            if (_score > _highScore)
            {
                _highScore = _score;
                PlayerPrefs.SetInt("BlockPuzzle_HighScore", _highScore);
                PlayerPrefs.Save();
                Debug.Log($"[Score] 新最高分: {_highScore}");
                OnHighScoreChanged?.Invoke(_highScore);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 放置方块加分（每格 1 分），记录本次放置得分供消除公式使用
        /// </summary>
        public void AddPlacementScore(int cellCount)
        {
            _lastPlacementScore = cellCount * Constants.ScorePerCell;
            _score += _lastPlacementScore;
            OnScoreChanged?.Invoke(_score);
        }

        /// <summary>
        /// 消除行/列加分（排分制 + 3次窗口 Combo 连击）
        /// 公式：总分 += B*排分 + 排数*[20*Combo*(1+排数)]
        /// 排分：1排=1, 2排=3, 3排=5, 4排=7, 5排=9
        ///
        /// Combo 规则（设计文档 118-124）：
        /// - 首次消除 → 开启3次窗口（本次无Combo加成，为后续做准备）
        /// - 窗口内消除 → 有Combo加成，窗口-1，Combo += 排数
        /// - 窗口耗尽(0) → Combo归零，下次消除开启新的一轮3次窗口
        /// </summary>
        public void AddLineClearScore(int lineCount)
        {
            if (lineCount <= 0)
            {
                ResetCombo();
                return;
            }

            // 排分：2*lineCount - 1 （1→1, 2→3, 3→5, 4→7, 5→9）
            int tierScore = lineCount * 2 - 1;

            // B * 排分（始终有效，无论是否在 Combo 窗口内）
            long placementBonus = (long)_lastPlacementScore * tierScore;

            // Combo 部分
            long comboPart = 0;
            int comboBefore = _comboCount; // 用于飘字显示

            if (_comboWindowRemain > 0)
            {
                // === 窗口内：有 Combo 加成 ===
                comboPart = lineCount * (20L * _comboCount * (1 + lineCount));
                _comboCount += lineCount;       // 消除N排 = Combo+N
                _comboWindowRemain--;           // 消耗 1 次窗口机会

                Debug.Log($"[Score] 消除 {lineCount} 排 | 窗口内({(_comboWindowRemain+1)}→{_comboWindowRemain}) | " +
                          $"B={_lastPlacementScore}×{tierScore}={placementBonus} | " +
                          $"Combo={comboBefore}×[20×(1+{lineCount})]×{lineCount}={comboPart}");
            }
            else
            {
                // === 窗口已耗尽：无 Combo 加成，开启新的一轮 3 次窗口 ===
                _comboCount = 0;
                _comboWindowRemain = 3;         // 新窗口（后续3次消除可触发 Combo）
                comboBefore = 0;

                Debug.Log($"[Score] 消除 {lineCount} 排 | 窗口耗尽→重开(剩余3) | " +
                          $"B={_lastPlacementScore}×{tierScore}={placementBonus} | 无Combo加成");
            }

            long totalAdd = placementBonus + comboPart;
            _score += (int)Mathf.Min(totalAdd, int.MaxValue);

            // 发出详细事件供飘字系统使用
            OnLineClearScoreDetail?.Invoke(lineCount, placementBonus, comboPart, comboBefore);
            OnComboChanged?.Invoke(_comboCount);

            // 更新总分显示
            OnScoreChanged?.Invoke(_score);
        }

        /// <summary>
        /// 重置 Combo（游戏结束时调用）。
        /// 注意：放置后未消除不再重置 Combo（设计文档 118-124 窗口机制下，
        /// 只有 3 次消除机会用完才会自然结束 Combo 周期）。
        /// </summary>
        public void ResetCombo()
        {
            if (_comboCount > 0 || _comboWindowRemain > 0)
            {
                _comboCount = 0;
                _comboWindowRemain = 0;
                OnComboChanged?.Invoke(_comboCount);
            }
        }
    }
}
