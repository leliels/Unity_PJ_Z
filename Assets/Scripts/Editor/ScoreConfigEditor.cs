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
        private SerializedProperty _clearBaseScoreMin;
        private SerializedProperty _clearBaseScoreMax;
        private SerializedProperty _cellBaseScoreMultiplier;
        private SerializedProperty _lineScoreMultipliers;
        private SerializedProperty _comboInitialValue;
        private SerializedProperty _comboGainPerClearedLine;
        private SerializedProperty _comboCooldownDefault;
        private SerializedProperty _maxScoreClamp;

        private void OnEnable()
        {
            _clearBaseScoreMin = serializedObject.FindProperty("_clearBaseScoreMin");
            _clearBaseScoreMax = serializedObject.FindProperty("_clearBaseScoreMax");
            _cellBaseScoreMultiplier = serializedObject.FindProperty("_cellBaseScoreMultiplier");
            _lineScoreMultipliers = serializedObject.FindProperty("_lineScoreMultipliers");
            _comboInitialValue = serializedObject.FindProperty("_comboInitialValue");
            _comboGainPerClearedLine = serializedObject.FindProperty("_comboGainPerClearedLine");
            _comboCooldownDefault = serializedObject.FindProperty("_comboCooldownDefault");
            _maxScoreClamp = serializedObject.FindProperty("_maxScoreClamp");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "这是计分规则配置资源。具体使用哪个配置，由 Boot 场景中 SceneBootstrap 的“本场景使用的计分配置”指定；如果未指定，则默认加载 Resources/Configs/ScoreConfig。",
                MessageType.Info);

            DrawHeader("消除计分");
            DrawProperty(_clearBaseScoreMin, "消除基本分最小值", "B 的随机区间下限。默认与最大值相同，表示固定 20 分。");
            DrawProperty(_clearBaseScoreMax, "消除基本分最大值", "B 的随机区间上限。如需轻微随机，可配置为 21，并将最小值配置为 19。");
            DrawProperty(_cellBaseScoreMultiplier, "格子基础分倍率", "D，用于放大或缩小本次方块占格数 C 的贡献。");
            DrawProperty(_lineScoreMultipliers, "排分倍率表", "根据本次消除排数 L 查表得到 R。例如消除 2 排使用第 2 档倍率。", true);

            DrawHeader("Combo");
            DrawProperty(_comboInitialValue, "Combo 初始值/最小值", "M 的初始值和重置值。当前规则固定最小为 1。");
            DrawProperty(_comboGainPerClearedLine, "Combo 增长值", "N，每消除 1 排增加多少 Combo 数；消除 L 排时增加 L × N。");
            DrawProperty(_comboCooldownDefault, "Combo CD 默认值", "消除后 CCD 重置到该值；未消除时逐次递减，归零后 Combo 数重置为初始值。");

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
