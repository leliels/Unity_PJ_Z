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

        [Header("消除计分")]
        [SerializeField]
        [InspectorName("消除基本分最小值")]
        [Tooltip("B 的随机区间下限。默认与最大值相同，表示固定 20 分。")]
        [Min(0)]
        private int _clearBaseScoreMin = 20;

        [SerializeField]
        [InspectorName("消除基本分最大值")]
        [Tooltip("B 的随机区间上限。如需轻微随机，可配置为 21，并将最小值配置为 19。")]
        [Min(0)]
        private int _clearBaseScoreMax = 20;

        [SerializeField]
        [InspectorName("格子基础分倍率")]
        [Tooltip("D，用于放大或缩小本次方块占格数 C 的贡献。")]
        [Min(0)]
        private int _cellBaseScoreMultiplier = 1;

        [SerializeField]
        [InspectorName("排分倍率表")]
        [Tooltip("根据本次消除排数 L 查表得到 R。例如消除 2 排使用第 2 档倍率。")]
        private int[] _lineScoreMultipliers = { 1, 3, 5, 7, 9 };

        [Header("Combo")]
        [SerializeField]
        [InspectorName("Combo 初始值/最小值")]
        [Tooltip("M 的初始值和重置值。当前规则固定最小为 1。")]
        [Min(1)]
        private int _comboInitialValue = 1;

        [SerializeField]
        [InspectorName("Combo 增长值")]
        [Tooltip("N，每消除 1 排增加多少 Combo 数；消除 L 排时增加 L × N。")]
        [Min(0)]
        private int _comboGainPerClearedLine = 1;

        [SerializeField]
        [InspectorName("Combo CD 默认值")]
        [Tooltip("消除后 CCD 重置到该值；未消除时逐次递减，归零后 Combo 数重置为初始值。")]
        [Min(0)]
        private int _comboCooldownDefault = 3;

        [Header("安全限制")]
        [SerializeField]
        [InspectorName("分数上限")]
        [Tooltip("防止公式配置过大导致分数溢出。")]
        [Min(1)]
        private int _maxScoreClamp = int.MaxValue;

        public int ClearBaseScoreMin => Mathf.Max(0, _clearBaseScoreMin);
        public int ClearBaseScoreMax => Mathf.Max(ClearBaseScoreMin, _clearBaseScoreMax);
        public int CellBaseScoreMultiplier => Mathf.Max(0, _cellBaseScoreMultiplier);
        public int ComboInitialValue => Mathf.Max(1, _comboInitialValue);
        public int ComboGainPerClearedLine => Mathf.Max(0, _comboGainPerClearedLine);
        public int ComboCooldownDefault => Mathf.Max(0, _comboCooldownDefault);
        public int MaxScoreClamp => Mathf.Max(1, _maxScoreClamp);

        public int GetClearBaseScore()
        {
            int min = ClearBaseScoreMin;
            int max = ClearBaseScoreMax;
            if (min == max) return min;
            return Random.Range(min, max == int.MaxValue ? int.MaxValue : max + 1);
        }

        public int GetLineScoreMultiplier(int lineCount)
        {
            if (lineCount <= 0) return 0;

            EnsureLineScoreMultipliers();

            int index = lineCount - 1;
            if (index < _lineScoreMultipliers.Length)
                return Mathf.Max(0, _lineScoreMultipliers[index]);

            return Mathf.Max(0, _lineScoreMultipliers[_lineScoreMultipliers.Length - 1]);
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
            _clearBaseScoreMin = 20;
            _clearBaseScoreMax = 20;
            _cellBaseScoreMultiplier = 1;
            _lineScoreMultipliers = new[] { 1, 3, 5, 7, 9 };
            _comboInitialValue = 1;
            _comboGainPerClearedLine = 1;
            _comboCooldownDefault = 3;
            _maxScoreClamp = int.MaxValue;
        }

        private void EnsureLineScoreMultipliers()
        {
            if (_lineScoreMultipliers == null || _lineScoreMultipliers.Length == 0)
                _lineScoreMultipliers = new[] { 1, 3, 5, 7, 9 };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureLineScoreMultipliers();

            _clearBaseScoreMin = Mathf.Max(0, _clearBaseScoreMin);
            _clearBaseScoreMax = Mathf.Max(_clearBaseScoreMin, _clearBaseScoreMax);
            _cellBaseScoreMultiplier = Mathf.Max(0, _cellBaseScoreMultiplier);
            for (int i = 0; i < _lineScoreMultipliers.Length; i++)
                _lineScoreMultipliers[i] = Mathf.Max(0, _lineScoreMultipliers[i]);

            _comboInitialValue = Mathf.Max(1, _comboInitialValue);
            _comboGainPerClearedLine = Mathf.Max(0, _comboGainPerClearedLine);
            _comboCooldownDefault = Mathf.Max(0, _comboCooldownDefault);
            _maxScoreClamp = Mathf.Max(1, _maxScoreClamp);
        }
#endif
    }
}
