using System;

namespace BlockPuzzle.Score
{
    /// <summary>
    /// Combo 运行时状态。
    /// </summary>
    public struct ScoreComboState
    {
        public int ComboCount;
        public int ComboRewardChanceRemain;

        public void Reset()
        {
            ComboCount = 0;
            ComboRewardChanceRemain = 0;
        }
    }

    /// <summary>
    /// 单次消除计分结果。
    /// </summary>
    public struct ScoreCalculationResult
    {
        public int LineCount;
        public int PlacementScore;
        public int LineTierScore;
        public long ClearBaseScore;
        public long ComboBonusScore;
        public int ComboCountUsedForBonus;
        public ScoreComboState ComboStateAfter;

        public long TotalLineClearScore => ClearBaseScore + ComboBonusScore;
    }

    /// <summary>
    /// 纯计分计算模块，不持有 MonoBehaviour 状态。
    /// </summary>
    public static class ScoreCalculator
    {
        public static ScoreCalculationResult CalculateLineClear(
            int lineCount,
            int placementScore,
            ScoreComboState comboState,
            ScoreConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var result = new ScoreCalculationResult
            {
                LineCount = Math.Max(0, lineCount),
                PlacementScore = Math.Max(0, placementScore),
                ComboStateAfter = comboState
            };

            if (lineCount <= 0)
                return result;

            result.LineTierScore = config.GetLineTierScore(lineCount);
            result.ClearBaseScore = (long)result.PlacementScore * result.LineTierScore;

            if (!config.EnableCombo)
            {
                result.ComboStateAfter.Reset();
                return result;
            }

            ScoreComboState nextState = comboState;
            bool hasActiveRound = nextState.ComboCount > 0 || nextState.ComboRewardChanceRemain > 0;
            int comboGain = lineCount * config.ComboGainPerClearedLine;

            if (!hasActiveRound)
            {
                nextState.Reset();
                nextState.ComboRewardChanceRemain = config.ComboRewardChanceLimit;

                if (config.ComboAppliesOnFirstClear)
                {
                    nextState.ComboCount = comboGain;
                    result.ComboCountUsedForBonus = nextState.ComboCount;
                    TryApplyComboBonus(lineCount, result.ComboCountUsedForBonus, config, ref nextState, ref result);
                }
                else
                {
                    nextState.ComboCount += comboGain;
                }
            }
            else
            {
                result.ComboCountUsedForBonus = nextState.ComboCount;
                TryApplyComboBonus(lineCount, result.ComboCountUsedForBonus, config, ref nextState, ref result);
                nextState.ComboCount += comboGain;
            }

            if (config.ComboChanceRecoverPerClear > 0 && config.ComboRewardChanceLimit > 0)
            {
                nextState.ComboRewardChanceRemain = Math.Min(
                    config.ComboRewardChanceLimit,
                    nextState.ComboRewardChanceRemain + config.ComboChanceRecoverPerClear);
            }

            if (nextState.ComboRewardChanceRemain <= 0)
                nextState.Reset();

            result.ComboStateAfter = nextState;
            return result;
        }

        private static void TryApplyComboBonus(
            int lineCount,
            int comboCountUsedForBonus,
            ScoreConfig config,
            ref ScoreComboState nextState,
            ref ScoreCalculationResult result)
        {
            if (nextState.ComboRewardChanceRemain <= 0 || comboCountUsedForBonus <= 0)
                return;

            result.ComboBonusScore = lineCount * (long)config.ComboBonusFactor * comboCountUsedForBonus * (1 + lineCount);
            nextState.ComboRewardChanceRemain = Math.Max(
                0,
                nextState.ComboRewardChanceRemain - config.ComboChanceCostPerTrigger);
        }
    }
}
