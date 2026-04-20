using UnityEngine;

namespace BlockPuzzle.Utils
{
    /// <summary>
    /// 全局常量定义
    /// </summary>
    public static class Constants
    {
        // --- 棋盘 ---
        public const int BoardRows = 8;
        public const int BoardCols = 8;
        public const float CellSize = 0.9f;       // 每个格子的世界单位大小
        public const float CellSpacing = 0.05f;    // 格子间距

        // --- 候选方块 ---
        public const int CandidateCount = 3;       // 候选区方块数量
        public const float CandidateScale = 0.6f;  // 候选方块缩放比例
        public const float CandidateSpacing = 3.0f;// 候选方块间距

        // --- 计分规则 ---
        public const int ScorePerCell = 1;          // 放置每格分数
        public static readonly int[] LineClearScore = { 0, 10, 30, 60, 100 };
        // index: 消除行/列数, value: 分数 (4+统一100)

        // --- 布局位置 ---
        public static readonly Vector3 BoardCenter = new Vector3(0f, 1.5f, 0f);
        public static readonly Vector3 CandidateCenter = new Vector3(0f, -5.5f, 0f);

        // --- 颜色 ---
        public static readonly Color PreviewValidColor = new Color(1f, 1f, 1f, 0.4f);
        public static readonly Color PreviewInvalidColor = new Color(1f, 0.3f, 0.3f, 0.4f);
        public static readonly Color CellEmptyColor = new Color(0.2f, 0.2f, 0.25f, 1f);

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
        /// 根据消除行/列数获取分数
        /// </summary>
        public static int GetLineClearScore(int lineCount)
        {
            if (lineCount <= 0) return 0;
            if (lineCount >= LineClearScore.Length) return LineClearScore[LineClearScore.Length - 1];
            return LineClearScore[lineCount];
        }
    }
}
