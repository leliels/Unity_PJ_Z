using UnityEngine;

namespace BlockPuzzle.Config
{
    /// <summary>
    /// 布局配置：棋盘、候选区、UI 在屏幕上的相对尺寸与位置。
    /// 所有数值都使用"屏幕宽/高的百分比"或"棋盘格相对单位",彻底脱离世界坐标 / 像素硬编码,
    /// 这样在 iPhone / iPad / 不同安卓机型上自动按比例适配。
    /// </summary>
    [CreateAssetMenu(fileName = "LayoutConfig", menuName = "BlockPuzzle/游戏配置/布局配置")]
    public sealed class LayoutConfig : ScriptableObject
    {
        public const string ResourcesPath = "Configs/01_Gameplay/LayoutConfig";

        // ==================== 参考分辨率 ====================
        [Header("参考分辨率")]
        [Tooltip("UI 设计稿基准分辨率（竖屏宽 x 高）。所有 Canvas Scaler 共用,确保多机型一致。")]
        [SerializeField] private Vector2 _referenceResolution = new Vector2(1080f, 1920f);

        [Tooltip("Canvas Scaler Match 值。0=按宽适配,1=按高适配,0.5=折中。竖屏游戏推荐 0.5。")]
        [Range(0f, 1f)]
        [SerializeField] private float _matchWidthOrHeight = 0.5f;

        // ==================== 棋盘 ====================
        [Header("棋盘")]
        [Tooltip("棋盘行数。当前玩法逻辑固定为 8,改这里仅作为未来扩展占位。")]
        [Range(4, 12)]
        [SerializeField] private int _boardRows = 8;

        [Tooltip("棋盘列数。当前玩法逻辑固定为 8,改这里仅作为未来扩展占位。")]
        [Range(4, 12)]
        [SerializeField] private int _boardCols = 8;

        [Tooltip("棋盘距离屏幕顶部的留白比例（占屏幕高度的百分比）。0.18 表示棋盘上方留约 18% 高度给 HUD。")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _boardMarginTopRatio = 0.18f;

        [Tooltip("棋盘距离屏幕底部的留白比例（占屏幕高度的百分比）。0.30 表示棋盘下方留约 30% 高度给候选区。")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _boardMarginBottomRatio = 0.30f;

        [Tooltip("棋盘格子之间的间隔占单元格大小的比例。0.06 表示间隔 = 单元格 × 6%。0 = 格子紧贴无间距。")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _cellSpacingRatio = 0.06f;

        // ==================== 棋盘位置偏移 ====================
        [Header("棋盘位置微调")]
        [Tooltip("棋盘中心水平偏移(占屏幕宽度的百分比)。正值=右移,负值=左移。0=居中。")]
        [Range(-0.3f, 0.3f)]
        [SerializeField] private float _boardOffsetXRatio = 0f;

        [Tooltip("棋盘中心垂直偏移(占屏幕高度的百分比)。正值=上移,负值=下移。0=不偏移(由留白决定位置)。")]
        [Range(-0.3f, 0.3f)]
        [SerializeField] private float _boardOffsetYRatio = 0f;

        // ==================== 候选区 ====================
        [Header("候选区")]
        [Tooltip("候选方块槽位数量。当前玩法是 3 个用完刷新。")]
        [Range(1, 5)]
        [SerializeField] private int _candidateSlotCount = 3;

        [Tooltip("候选方块在槽位中相对于一个棋盘格的缩放比例。0.85 表示候选方块格 = 棋盘格 × 0.85。")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _candidateBlockScale = 0.85f;

        [Tooltip("候选区距屏幕底部的留白比例（占屏幕高度的百分比）。")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _candidateBottomMarginRatio = 0.05f;

        // ==================== Public 访问器 ====================
        public Vector2 ReferenceResolution => _referenceResolution;
        public float MatchWidthOrHeight => _matchWidthOrHeight;
        public int BoardRows => _boardRows;
        public int BoardCols => _boardCols;
        public float BoardMarginTopRatio => _boardMarginTopRatio;
        public float BoardMarginBottomRatio => _boardMarginBottomRatio;
        public float CellSpacingRatio => _cellSpacingRatio;
        public float BoardOffsetXRatio => _boardOffsetXRatio;
        public float BoardOffsetYRatio => _boardOffsetYRatio;
        public int CandidateSlotCount => _candidateSlotCount;
        public float CandidateBlockScale => _candidateBlockScale;
        public float CandidateBottomMarginRatio => _candidateBottomMarginRatio;
    }
}
