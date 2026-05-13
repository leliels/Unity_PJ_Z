using UnityEngine;

namespace BlockPuzzle.Utils
{
    /// <summary>
    /// 全局常量。
    /// M-R7 后:这里只放真正不可变的逻辑常量。所有可调参数(布局/配色/动效)都放在 LayoutConfig / UIThemeConfig / GameplayTuning。
    /// 不要再在这里加 static 可变字段。
    /// </summary>
    public static class Constants
    {
        // --- 棋盘逻辑常量(玩法层硬约束,不可调) ---
        public const int BoardRows = 8;
        public const int BoardCols = 8;
        public const int CandidateCount = 3;

        // --- 默认配色(仅 fallback,UIThemeConfig 优先) ---
        public static readonly Color PreviewValidColor = new Color(1f, 1f, 1f, 0.4f);
        public static readonly Color PreviewInvalidColor = new Color(1f, 0.3f, 0.3f, 0.4f);
        public static readonly Color CellEmptyColor = new Color(0.2f, 0.2f, 0.25f, 0f);
        public static readonly Color ClearPreviewHighlightColor = new Color(1f, 1f, 1f, 0.6f);

        public static readonly Color[] BlockColors = new Color[]
        {
            new Color(0.95f, 0.30f, 0.30f, 1f),
            new Color(0.95f, 0.60f, 0.20f, 1f),
            new Color(0.95f, 0.90f, 0.25f, 1f),
            new Color(0.30f, 0.85f, 0.40f, 1f),
            new Color(0.30f, 0.55f, 0.95f, 1f),
            new Color(0.70f, 0.35f, 0.90f, 1f),
        };
    }
}
