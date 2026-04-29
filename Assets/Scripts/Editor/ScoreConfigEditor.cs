using BlockPuzzle.Score;
using UnityEditor;
using UnityEngine;

namespace BlockPuzzle.Editor
{
    /// <summary>
    /// ScoreConfig 专用 Inspector，确保配置项以中文名和中文悬浮说明显示。
    /// </summary>
    [CustomEditor(typeof(ScoreConfig))]
    public sealed class ScoreConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _placementScorePerCell;
        private SerializedProperty _lineTierScores;
        private SerializedProperty _extraLineTierStep;
        private SerializedProperty _enableCombo;
        private SerializedProperty _comboBonusFactor;
        private SerializedProperty _comboRewardChanceLimit;
        private SerializedProperty _comboChanceCostPerTrigger;
        private SerializedProperty _comboChanceRecoverPerClear;
        private SerializedProperty _comboAppliesOnFirstClear;
        private SerializedProperty _comboGainPerClearedLine;
        private SerializedProperty _resetComboOnNoClear;
        private SerializedProperty _maxScoreClamp;

        private void OnEnable()
        {
            _placementScorePerCell = serializedObject.FindProperty("_placementScorePerCell");
            _lineTierScores = serializedObject.FindProperty("_lineTierScores");
            _extraLineTierStep = serializedObject.FindProperty("_extraLineTierStep");
            _enableCombo = serializedObject.FindProperty("_enableCombo");
            _comboBonusFactor = serializedObject.FindProperty("_comboBonusFactor");
            _comboRewardChanceLimit = serializedObject.FindProperty("_comboRewardChanceLimit");
            _comboChanceCostPerTrigger = serializedObject.FindProperty("_comboChanceCostPerTrigger");
            _comboChanceRecoverPerClear = serializedObject.FindProperty("_comboChanceRecoverPerClear");
            _comboAppliesOnFirstClear = serializedObject.FindProperty("_comboAppliesOnFirstClear");
            _comboGainPerClearedLine = serializedObject.FindProperty("_comboGainPerClearedLine");
            _resetComboOnNoClear = serializedObject.FindProperty("_resetComboOnNoClear");
            _maxScoreClamp = serializedObject.FindProperty("_maxScoreClamp");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "这是计分规则配置资源。具体使用哪个配置，由 Boot 场景中 SceneBootstrap 的“本场景使用的计分配置”指定；如果未指定，则默认加载 Resources/Configs/ScoreConfig。",
                MessageType.Info);

            DrawHeader("基础计分");
            DrawProperty(_placementScorePerCell, "每格放置分", "每个被方块占用的棋盘格，放置成功后立即获得多少分。");
            DrawProperty(_lineTierScores, "排分表", "根据本次消除排数查表得到排分。例如消除 2 排使用第 2 档排分。", true);
            DrawProperty(_extraLineTierStep, "超出排分递增值", "当消除排数超过排分表长度时，每多 1 排额外增加多少排分。默认 2 表示继续 1、3、5、7、9、11...。");

            DrawHeader("Combo 计分");
            DrawProperty(_enableCombo, "启用 Combo", "是否启用 Combo 连击加成；关闭后只计算放置分和消除基础加分。");
            DrawProperty(_comboBonusFactor, "Combo 加成系数", "来自当前公式中的 20，用于控制 Combo 额外分整体强度。它不是 Combo 数，也不是奖励机会数。");
            DrawProperty(_comboRewardChanceLimit, "每轮 Combo 奖励机会数", "一轮 Combo 开启后，最多允许多少次后续消除获得 Combo 加成。");
            DrawProperty(_comboChanceCostPerTrigger, "每次触发消耗机会数", "每次实际获得 Combo 加成后，从奖励机会数中扣除多少次。");
            DrawProperty(_comboChanceRecoverPerClear, "每次消除补充机会数", "每次消除后给奖励机会数补回多少；默认 0，表示不会越连越延长。");
            DrawProperty(_comboAppliesOnFirstClear, "首次消除是否计算 Combo", "开启后，首次消除也会参与 Combo 加成；关闭时首次消除只开启 Combo 轮次。");
            DrawProperty(_comboGainPerClearedLine, "每排 Combo 增长值", "每消除 1 排增加多少 Combo 数；消除 N 排时增加 N × 此值。");
            DrawProperty(_resetComboOnNoClear, "未消除是否重置 Combo", "放置方块但没有消除时，是否立即清空 Combo 数和奖励机会数。默认开启。");

            DrawHeader("安全限制");
            DrawProperty(_maxScoreClamp, "分数上限", "防止公式配置过大导致分数溢出。");

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawHeader(string text)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }

        private static void DrawProperty(SerializedProperty property, string label, string tooltip, bool includeChildren = false)
        {
            if (property == null) return;
            EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip), includeChildren);
        }
    }
}
