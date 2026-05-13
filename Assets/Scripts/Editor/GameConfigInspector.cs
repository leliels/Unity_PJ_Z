using UnityEngine;
using UnityEditor;
using BlockPuzzle.Config;

namespace BlockPuzzle.EditorTools
{
    /// <summary>
    /// GameConfig 自定义 Inspector：
    /// - 顶部一段中文使用说明 HelpBox
    /// - 默认按 SerializedObject 绘制各子 SO 字段(Unity 自带 ObjectField,无须重新发明)
    /// - 在每个子 SO 字段右侧加一个"打开"按钮,直接 ping/select 对应 .asset
    /// - 底部显示运行时校验缺失提示
    /// </summary>
    [CustomEditor(typeof(GameConfig))]
    public sealed class GameConfigInspector : UnityEditor.Editor
    {
        private static readonly (string field, string label, string hint)[] OpenButtons = new (string, string, string)[]
        {
            ("_score",       "计分配置",      "ScoreConfig.asset"),
            ("_shapes",      "方块形状库",    "DefaultBlockShapeDatabase.asset"),
            ("_layout",      "布局配置",      "LayoutConfig.asset"),
            ("_theme",       "UI 主题",       "UIThemeConfig.asset"),
            ("_gameplay",    "玩法微调",      "GameplayTuning.asset"),
            ("_modeCatalog", "模式目录",      "ModeCatalog.asset"),
            ("_defaultMode", "默认模式",      "Mode_Traditional.asset"),
            ("_audioLibrary","音效素材库",    "AudioLibrary.asset"),
        };

        public override void OnInspectorGUI()
        {
            DrawHelpBox();
            EditorGUILayout.Space(4);

            serializedObject.Update();

            DrawScriptField();

            // 用我们自己的"字段 + 打开按钮"绘制,不走 DrawDefaultInspector
            // 这样美术/策划点字段右侧的"打开"就能直接跳转到对应 .asset
            foreach (var (field, label, _) in OpenButtons)
            {
                DrawFieldWithOpenButton(field);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            DrawValidationFooter();
        }

        // ==================== 子绘制 ====================

        private static void DrawHelpBox()
        {
            EditorGUILayout.HelpBox(
                "【游戏配置中心】这是整个游戏的配置入口。\n" +
                "• 想改棋盘大小、留白 → 点开\"布局配置\"右侧的\"打开\"按钮\n" +
                "• 想改计分公式 → 点\"计分配置\"右侧\"打开\"\n" +
                "• 想换音效/特效/飘字 → 进入对应 SO,无需进代码\n" +
                "• 任何字段标错都不会让游戏崩溃,启动时会在 Console 提示。",
                MessageType.Info);
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromScriptableObject((ScriptableObject)target),
                    typeof(MonoScript),
                    false);
            }
        }

        private void DrawFieldWithOpenButton(string fieldName)
        {
            var prop = serializedObject.FindProperty(fieldName);
            if (prop == null) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(prop, true);

                using (new EditorGUI.DisabledScope(prop.objectReferenceValue == null))
                {
                    if (GUILayout.Button("打开", GUILayout.Width(50)))
                    {
                        var asset = prop.objectReferenceValue;
                        if (asset != null)
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }
                    }
                }
            }
        }

        private void DrawValidationFooter()
        {
            var config = (GameConfig)target;
            var missing = config.ValidateRuntime();
            if (missing == null || missing.Length == 0)
            {
                EditorGUILayout.HelpBox("✓ 所有关键配置已就绪。", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "下列关键配置为空,游戏可能无法正常启动:\n  · " + string.Join("\n  · ", missing),
                MessageType.Warning);
        }
    }
}
