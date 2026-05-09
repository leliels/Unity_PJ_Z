using System;

namespace BlockPuzzle.Score
{
    /// <summary>
    /// Combo 运行时状态。
    /// </summary>
    public struct ScoreComboState
    {
        public int ComboCount;
        public int ComboCooldownRemaining;

        public void Reset(ScoreConfig config)
        {
            ComboCount = config != null ? config.ComboInitialValue : 0;
            ComboCooldownRemaining = config != null ? config.ComboCooldownDefault : 3;
        }
    }

    /// <summary>
    /// 单次计分结果。
    /// </summary>
    public struct ScoreCalculationResult
    {
        public int LineCount;
        public int PlacedCellCount;
        public int ClearBaseScore;
        public int CellBaseScoreMultiplier;
        public int LineScoreMultiplier;
        public int ComboCountUsed;
        public long CellScore;
        public long ClearComboScore;
        public ScoreComboState ComboStateAfter;

        public long TotalScore => CellScore + ClearComboScore;
    }

    /// <summary>
    /// 纯计分计算模块，不持有 MonoBehaviour 状态。
    /// </summary>
    public static class ScoreCalculator
    {
        public static ScoreCalculationResult CalculateLineClear(
            int lineCount,
            int placedCellCount,
            ScoreComboState comboState,
            ScoreConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            int safeLineCount = Math.Max(0, lineCount);
            int safePlacedCellCount = Math.Max(0, placedCellCount);
            ScoreComboState nextState = comboState;
            if (nextState.ComboCount < config.ComboInitialValue)
                nextState.ComboCount = config.ComboInitialValue;

            var result = new ScoreCalculationResult
            {
                LineCount = safeLineCount,
                PlacedCellCount = safePlacedCellCount,
                ComboStateAfter = nextState
            };

            if (safeLineCount <= 0)
                return result;

            result.ClearBaseScore = config.GetClearBaseScore();
            result.CellBaseScoreMultiplier = config.CellBaseScoreMultiplier;
            result.LineScoreMultiplier = config.GetLineScoreMultiplier(safeLineCount);

            int comboGain = safeLineCount * config.ComboGainPerClearedLine;

            if (nextState.ComboCount == 0)
            {
                // 首次消除（M=0）：不产生 Combo，倍率固定为 1
                // M 设为 1 仅标记"已消除过"，供后续消除判定
                nextState.ComboCount = 1;
                result.ComboCountUsed = 1;
            }
            else
            {
                // 后续消除（M>0）：Combo = 当前M + 本次消除排数×增长值
                nextState.ComboCount += comboGain;
                result.ComboCountUsed = nextState.ComboCount;
            }

            nextState.ComboCooldownRemaining = config.ComboCooldownDefault;
            result.CellScore = (long)safePlacedCellCount * result.CellBaseScoreMultiplier * result.LineScoreMultiplier;
            result.ClearComboScore = (long)result.ClearBaseScore * result.ComboCountUsed * safeLineCount * (safeLineCount + 1);
            result.ComboStateAfter = nextState;
            return result;
        }

        public static ScoreComboState CalculateNoClear(ScoreComboState comboState, ScoreConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            ScoreComboState nextState = comboState;
            if (nextState.ComboCount < config.ComboInitialValue)
                nextState.ComboCount = config.ComboInitialValue;

            nextState.ComboCooldownRemaining = Math.Max(0, nextState.ComboCooldownRemaining - 1);
            if (nextState.ComboCooldownRemaining <= 0)
            {
                nextState.ComboCooldownRemaining = 0;
                nextState.ComboCount = config.ComboInitialValue;
            }

            return nextState;
        }
    }
}
