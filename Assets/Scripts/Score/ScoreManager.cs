using System;
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Score
{
    /// <summary>
    /// 分数管理器：持有总分、最高分和 Combo 状态，具体计分公式由 ScoreConfig + ScoreCalculator 决定。
    /// </summary>
    public class ScoreManager : Singleton<ScoreManager>
    {
        /// <summary>分数变化事件（参数：当前总分）</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>Combo 变化事件（参数：当前 Combo 数）</summary>
        public event Action<int> OnComboChanged;

        /// <summary>消除计分详情事件（参数：消除排数、消除基础加分、Combo 加成分、本次用于加成的 Combo 数）</summary>
        public event Action<int, long, long, int> OnLineClearScoreDetail;

        /// <summary>最高分变化事件（参数：新的最高分）</summary>
        public event Action<int> OnHighScoreChanged;

        [Header("计分配置")]
        [SerializeField]
        [InspectorName("计分配置")]
        [Tooltip("计分规则配置。为空时会自动从 Resources/Configs/ScoreConfig 加载；仍为空则使用运行时默认值。")]
        private ScoreConfig _scoreConfig;

        private ScoreConfig _runtimeDefaultConfig;
        private bool _configWarningLogged;

        private int _score;
        public int CurrentScore => _score;

        private int _highScore;
        /// <summary>历史最高分（从 PlayerPrefs 加载）</summary>
        public int HighScore => _highScore;

        private ScoreComboState _comboState;
        /// <summary>当前 Combo 数（每轮结束或规则重置时归零）</summary>
        public int ComboCount => _comboState.ComboCount;
        /// <summary>当前轮剩余 Combo 奖励机会数</summary>
        public int ComboRewardChanceRemain => _comboState.ComboRewardChanceRemain;

        /// <summary>本次放置的方块得分（供消除公式和飘字表现使用）</summary>
        private int _lastPlacementScore;
        public int LastPlacementScore => _lastPlacementScore;

        public ScoreConfig Config
        {
            get
            {
                EnsureConfig();
                return _scoreConfig != null ? _scoreConfig : _runtimeDefaultConfig;
            }
        }

        /// <summary>
        /// 设置本局使用的计分配置。通常由 SceneBootstrap 在启动时注入。
        /// </summary>
        public void SetConfig(ScoreConfig scoreConfig)
        {
            if (scoreConfig == null) return;
            _scoreConfig = scoreConfig;
            _configWarningLogged = false;
        }

        protected override void Awake()
        {
            base.Awake();
            EnsureConfig();
        }

        /// <summary>
        /// 初始化/重置分数和 Combo（不重置最高分）。
        /// </summary>
        public void ResetScore()
        {
            _score = 0;
            _lastPlacementScore = 0;
            _comboState.Reset();

            // 首次或重置时从 PlayerPrefs 加载最高分
            if (_highScore == 0)
                _highScore = PlayerPrefs.GetInt("BlockPuzzle_HighScore", 0);

            OnScoreChanged?.Invoke(_score);
            OnComboChanged?.Invoke(_comboState.ComboCount);
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
        /// 放置方块加分，并记录本次放置得分供消除公式使用。
        /// </summary>
        public void AddPlacementScore(int cellCount)
        {
            _lastPlacementScore = Math.Max(0, cellCount) * Config.PlacementScorePerCell;
            AddScore(_lastPlacementScore);
            OnScoreChanged?.Invoke(_score);
        }

        /// <summary>
        /// 消除行/列加分（排分制 + 可配置 Combo）。
        /// </summary>
        public void AddLineClearScore(int lineCount)
        {
            if (lineCount <= 0)
            {
                OnTurnCompletedWithoutClear();
                return;
            }

            ScoreCalculationResult result = ScoreCalculator.CalculateLineClear(
                lineCount,
                _lastPlacementScore,
                _comboState,
                Config);

            _comboState = result.ComboStateAfter;
            AddScore(result.TotalLineClearScore);

            Debug.Log($"[Score] 消除 {lineCount} 排 | " +
                      $"B={_lastPlacementScore}×排分{result.LineTierScore}={result.ClearBaseScore} | " +
                      $"Combo数={result.ComboCountUsedForBonus}, Combo加成={result.ComboBonusScore} | " +
                      $"剩余奖励机会={_comboState.ComboRewardChanceRemain}");

            // 发出详细事件供飘字系统使用
            OnLineClearScoreDetail?.Invoke(
                lineCount,
                result.ClearBaseScore,
                result.ComboBonusScore,
                result.ComboCountUsedForBonus);
            OnComboChanged?.Invoke(_comboState.ComboCount);
            OnScoreChanged?.Invoke(_score);
        }

        /// <summary>
        /// 本回合没有产生消除时调用。默认根据配置重置 Combo。
        /// </summary>
        public void OnTurnCompletedWithoutClear()
        {
            if (Config.ResetComboOnNoClear)
                ResetCombo();
        }

        /// <summary>
        /// 重置 Combo。
        /// </summary>
        public void ResetCombo()
        {
            if (_comboState.ComboCount > 0 || _comboState.ComboRewardChanceRemain > 0)
            {
                _comboState.Reset();
                OnComboChanged?.Invoke(_comboState.ComboCount);
            }
        }

        public int GetLineTierScore(int lineCount)
        {
            return Config.GetLineTierScore(lineCount);
        }

        private void AddScore(long amount)
        {
            if (amount <= 0) return;

            long nextScore = (long)_score + amount;
            nextScore = Math.Min(nextScore, Config.MaxScoreClamp);
            nextScore = Math.Max(0, nextScore);
            _score = (int)nextScore;
        }

        private void EnsureConfig()
        {
            if (_scoreConfig != null) return;

            _scoreConfig = Resources.Load<ScoreConfig>(ScoreConfig.ResourcesPath);
            if (_scoreConfig != null) return;

            if (_runtimeDefaultConfig == null)
                _runtimeDefaultConfig = ScoreConfig.CreateRuntimeDefault();

            if (!_configWarningLogged)
            {
                Debug.LogWarning("[Score] 未找到 Resources/Configs/ScoreConfig，已使用运行时默认计分配置。");
                _configWarningLogged = true;
            }
        }
    }
}

