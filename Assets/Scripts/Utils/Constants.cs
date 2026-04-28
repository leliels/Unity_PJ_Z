using UnityEngine;

namespace BlockPuzzle.Utils
{
    /// <summary>
    /// 全局常量与可配置参数。
    /// 布局类参数为 static（非 const），可由 SceneBootstrap 在 Awake 时从 Inspector 覆盖。
    /// </summary>
    public static class Constants
    {
        // --- 棋盘（逻辑，不可改） ---
        public const int BoardRows = 8;
        public const int BoardCols = 8;

        // --- 棋盘视觉（可在 Inspector 调整） ---
        public static float CellSize = 1.0f;              // 每个格子的世界单位大小
        public static float CellSpacing = 0.08f;       // 格子间距

        // --- 候选方块（可在 Inspector 调整） ---
        public const int CandidateCount = 3;            // 候选区方块数量
        public static float CandidateScale = 0.55f;     // 候选方块缩放比例
        public static float CandidateSpacing = 3.6f;    // 候选方块间距

        // --- 计分规则（2026-04-23 新版） ---
        public const int ScorePerCell = 1;               // 放置每格分数
        public const int LineClearBase = 16;              // 消除基数 = 棋盘列数(8) × 2
        public const float ComboMultiplierStep = 0.2f;    // 每次 Combo 增加的系数

        // --- 布局位置（可在 Inspector 调整） ---
        public static Vector3 BoardCenter = new Vector3(0f, 1.1f, 0f);
        public static Vector3 CandidateCenter = new Vector3(0f, -8.2f, 0f);

        // --- 颜色 ---
        public static readonly Color PreviewValidColor = new Color(1f, 1f, 1f, 0.4f);
        public static readonly Color PreviewInvalidColor = new Color(1f, 0.3f, 0.3f, 0.4f);
        public static readonly Color CellEmptyColor = new Color(0.2f, 0.2f, 0.25f, 0f);

        /// <summary>拖拽预览时，放置后会消除的行/列的高亮颜色（由 SceneBootstrap Inspector 覆盖）</summary>
        public static Color ClearPreviewHighlightColor = new Color(1f, 1f, 1f, 0.6f);

        /// <summary>
        /// 方块可用颜色（对应 SH1_0 ~ SH1_5）
        /// </summary>
        public static readonly Color[] BlockColors = new Color[]
        {
            new Color(0.95f, 0.30f, 0.30f, 1f), // 红
            new Color(0.95f, 0.60f, 0.20f, 1f), // 橙
            new Color(0.95f, 0.90f, 0.25f, 1f), // 黄
            new Color(0.30f, 0.85f, 0.40f, 1f), // 绿
            new Color(0.30f, 0.55f, 0.95f, 1f), // 蓝
            new Color(0.70f, 0.35f, 0.90f, 1f), // 紫
        };

        /// <summary>
        /// 根据消除行/列数计算消除分数（16^N 幂次公式）
        /// </summary>
        public static long GetLineClearScore(int lineCount)
        {
            if (lineCount <= 0) return 0;
            long score = 1;
            for (int i = 0; i < lineCount; i++)
                score *= LineClearBase;
            return score; // 1排=16, 2排=256, 3排=4096, 4排=65536
        }

        /// <summary>
        /// 根据 Combo 次数获取乘数（1 + N * 0.2）
        /// Combo 0 = 第一次消除，系数 1.0（无加成）
        /// Combo 1 = 第二次连续消除，系数 1.2
        /// </summary>
        public static float GetComboMultiplier(int comboCount)
        {
            return 1f + comboCount * ComboMultiplierStep;
        }
    }
}
