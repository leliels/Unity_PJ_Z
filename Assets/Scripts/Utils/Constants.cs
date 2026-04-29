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
        public static float CellSize = 1.17f;              // 每个格子的世界单位大小
        public static float CellSpacing = 0.09f;       // 格子间距

        // --- 候选方块（可在 Inspector 调整） ---
        public const int CandidateCount = 3;            // 候选区方块数量
        public static float CandidateScale = 0.35f;     // 候选方块缩放比例
        public static float CandidateSpacing = 3.5f;    // 候选方块间距

        // --- 布局位置（可在 Inspector 调整） ---
        public static Vector3 BoardCenter = new Vector3(0f, -0.1f, 0f);
        public static Vector3 CandidateCenter = new Vector3(0f, -8.5f, 0f);

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


    }
}
