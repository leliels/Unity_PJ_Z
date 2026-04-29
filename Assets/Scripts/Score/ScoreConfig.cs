using UnityEngine;

namespace BlockPuzzle.Score
{
    /// <summary>
    /// 计分规则配置。通过 ScriptableObject 暴露给策划在 Inspector 中调整。
    /// </summary>
    [CreateAssetMenu(fileName = "ScoreConfig", menuName = "BlockPuzzle/计分配置")]
    public sealed class ScoreConfig : ScriptableObject
    {
        public const string ResourcesPath = "Configs/ScoreConfig";

        [Header("基础计分")]
        [SerializeField]
        [InspectorName("每格放置分")]
        [Tooltip("每个被方块占用的棋盘格，放置成功后立即获得多少分。")]
        [Min(0)]
        private int _placementScorePerCell = 1;

        [SerializeField]
        [InspectorName("排分表")]
        [Tooltip("根据本次消除排数查表得到排分。例如消除 2 排使用第 2 档排分。")]
        private int[] _lineTierScores = { 1, 3, 5, 7, 9 };

        [SerializeField]
        [InspectorName("超出排分递增值")]
        [Tooltip("当消除排数超过排分表长度时，每多 1 排额外增加多少排分。默认 2 表示继续 1、3、5、7、9、11...。")]
        [Min(0)]
        private int _extraLineTierStep = 2;

        [Header("Combo 计分")]
        [SerializeField]
        [InspectorName("启用 Combo")]
        [Tooltip("是否启用 Combo 连击加成；关闭后只计算放置分和消除基础加分。")]
        private bool _enableCombo = true;

        [SerializeField]
        [InspectorName("Combo 加成系数")]
        [Tooltip("来自当前公式中的 20，用于控制 Combo 额外分整体强度。它不是 Combo 数，也不是奖励机会数。")]
        [Min(0)]
        private int _comboBonusFactor = 20;

        [SerializeField]
        [InspectorName("每轮 Combo 奖励机会数")]
        [Tooltip("一轮 Combo 开启后，最多允许多少次后续消除获得 Combo 加成。")]
        [Min(0)]
        private int _comboRewardChanceLimit = 3;

        [SerializeField]
        [InspectorName("每次触发消耗机会数")]
        [Tooltip("每次实际获得 Combo 加成后，从奖励机会数中扣除多少次。")]
        [Min(1)]
        private int _comboChanceCostPerTrigger = 1;

        [SerializeField]
        [InspectorName("每次消除补充机会数")]
        [Tooltip("每次消除后给奖励机会数补回多少；默认 0，表示不会越连越延长。")]
        [Min(0)]
        private int _comboChanceRecoverPerClear = 0;

        [SerializeField]
        [InspectorName("首次消除是否计算 Combo")]
        [Tooltip("开启后，首次消除也会参与 Combo 加成；关闭时首次消除只开启 Combo 轮次。")]
        private bool _comboAppliesOnFirstClear = false;

        [SerializeField]
        [InspectorName("每排 Combo 增长值")]
        [Tooltip("每消除 1 排增加多少 Combo 数；消除 N 排时增加 N × 此值。")]
        [Min(0)]
        private int _comboGainPerClearedLine = 1;

        [SerializeField]
        [InspectorName("未消除是否重置 Combo")]
        [Tooltip("放置方块但没有消除时，是否立即清空 Combo 数和奖励机会数。默认开启。")]
        private bool _resetComboOnNoClear = true;

        [Header("安全限制")]
        [SerializeField]
        [InspectorName("分数上限")]
        [Tooltip("防止公式配置过大导致分数溢出。")]
        [Min(1)]
        private int _maxScoreClamp = int.MaxValue;

        public int PlacementScorePerCell => Mathf.Max(0, _placementScorePerCell);
        public int ExtraLineTierStep => Mathf.Max(0, _extraLineTierStep);
        public bool EnableCombo => _enableCombo;
        public int ComboBonusFactor => Mathf.Max(0, _comboBonusFactor);
        public int ComboRewardChanceLimit => Mathf.Max(0, _comboRewardChanceLimit);
        public int ComboChanceCostPerTrigger => Mathf.Max(1, _comboChanceCostPerTrigger);
        public int ComboChanceRecoverPerClear => Mathf.Max(0, _comboChanceRecoverPerClear);
        public bool ComboAppliesOnFirstClear => _comboAppliesOnFirstClear;
        public int ComboGainPerClearedLine => Mathf.Max(0, _comboGainPerClearedLine);
        public bool ResetComboOnNoClear => _resetComboOnNoClear;
        public int MaxScoreClamp => Mathf.Max(1, _maxScoreClamp);

        public int GetLineTierScore(int lineCount)
        {
            if (lineCount <= 0) return 0;

            EnsureLineTierScores();

            int index = lineCount - 1;
            if (index < _lineTierScores.Length)
                return Mathf.Max(0, _lineTierScores[index]);

            int lastTier = Mathf.Max(0, _lineTierScores[_lineTierScores.Length - 1]);
            int extraCount = index - _lineTierScores.Length + 1;
            return lastTier + extraCount * ExtraLineTierStep;
        }

        public static ScoreConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<ScoreConfig>();
            config.name = "RuntimeDefaultScoreConfig";
            config.hideFlags = HideFlags.DontSave;
            config.ResetToDefaults();
            return config;
        }

        private void ResetToDefaults()
        {
            _placementScorePerCell = 1;
            _lineTierScores = new[] { 1, 3, 5, 7, 9 };
            _extraLineTierStep = 2;
            _enableCombo = true;
            _comboBonusFactor = 20;
            _comboRewardChanceLimit = 3;
            _comboChanceCostPerTrigger = 1;
            _comboChanceRecoverPerClear = 0;
            _comboAppliesOnFirstClear = false;
            _comboGainPerClearedLine = 1;
            _resetComboOnNoClear = true;
            _maxScoreClamp = int.MaxValue;
        }

        private void EnsureLineTierScores()
        {
            if (_lineTierScores == null || _lineTierScores.Length == 0)
                _lineTierScores = new[] { 1, 3, 5, 7, 9 };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureLineTierScores();

            _placementScorePerCell = Mathf.Max(0, _placementScorePerCell);
            for (int i = 0; i < _lineTierScores.Length; i++)
                _lineTierScores[i] = Mathf.Max(0, _lineTierScores[i]);

            _extraLineTierStep = Mathf.Max(0, _extraLineTierStep);
            _comboBonusFactor = Mathf.Max(0, _comboBonusFactor);
            _comboRewardChanceLimit = Mathf.Max(0, _comboRewardChanceLimit);
            _comboChanceCostPerTrigger = Mathf.Max(1, _comboChanceCostPerTrigger);
            _comboChanceRecoverPerClear = Mathf.Max(0, _comboChanceRecoverPerClear);
            _comboGainPerClearedLine = Mathf.Max(0, _comboGainPerClearedLine);
            _maxScoreClamp = Mathf.Max(1, _maxScoreClamp);
        }
#endif
    }
}
