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

        /// <summary>消除计分详情事件（参数：消除排数、格子得分项、消除/Combo 得分项、本次使用的 Combo 数）</summary>
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
        /// <summary>当前 Combo 数，最小值由 ScoreConfig 决定，默认 1。</summary>
        public int ComboCount => _comboState.ComboCount;
        /// <summary>当前剩余 Combo CD。</summary>
        public int ComboCooldownRemaining => _comboState.ComboCooldownRemaining;

        /// <summary>本次成功放置的方块占格数 C。</summary>
        private int _lastPlacedCellCount;
        public int LastPlacedCellCount => _lastPlacedCellCount;

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
            _lastPlacedCellCount = 0;
            _comboState.Reset(Config);

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
        /// 记录本次成功放置方块的占格数 C。普通放置不直接加分。
        /// </summary>
        public void RecordPlacedCells(int cellCount)
        {
            _lastPlacedCellCount = Math.Max(0, cellCount);
        }

        /// <summary>
        /// 消除行/列加分。
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
                _lastPlacedCellCount,
                _comboState,
                Config);

            _comboState = result.ComboStateAfter;
            AddScore(result.TotalScore);

            Debug.Log($"[Score] 消除 {lineCount} 排 | " +
                      $"格子项={result.PlacedCellCount}×{result.CellBaseScoreMultiplier}×{result.LineScoreMultiplier}={result.CellScore} | " +
                      $"消除/Combo项={result.ClearBaseScore}×{result.ComboCountUsed}×{result.LineCount}×{result.LineCount + 1}={result.ClearComboScore} | " +
                      $"CCD={_comboState.ComboCooldownRemaining}");

            OnLineClearScoreDetail?.Invoke(
                lineCount,
                result.CellScore,
                result.ClearComboScore,
                result.ComboCountUsed);
            OnComboChanged?.Invoke(_comboState.ComboCount);
            OnScoreChanged?.Invoke(_score);
        }

        /// <summary>
        /// 本回合没有产生消除时调用：当前分不变，CCD 递减；CCD 归零时 Combo 数重置为初始值。
        /// </summary>
        public void OnTurnCompletedWithoutClear()
        {
            int comboBefore = _comboState.ComboCount;
            int cooldownBefore = _comboState.ComboCooldownRemaining;
            _comboState = ScoreCalculator.CalculateNoClear(_comboState, Config);

            if (comboBefore != _comboState.ComboCount)
                OnComboChanged?.Invoke(_comboState.ComboCount);

            if (cooldownBefore != _comboState.ComboCooldownRemaining || comboBefore != _comboState.ComboCount)
                Debug.Log($"[Score] 未消除 | CCD={_comboState.ComboCooldownRemaining}, Combo={_comboState.ComboCount}");
        }

        public int GetLineScoreMultiplier(int lineCount)
        {
            return Config.GetLineScoreMultiplier(lineCount);
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
