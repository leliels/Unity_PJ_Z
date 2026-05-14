using UnityEngine;

namespace BlockPuzzle.Config
{
    /// <summary>
    /// UI 主题配置：颜色、字体、SafeArea 策略等"美术风格层"参数。
    /// 美术换肤、调色、改字体只动这一份。
    /// </summary>
    [CreateAssetMenu(fileName = "UIThemeConfig", menuName = "BlockPuzzle/游戏配置/UI 主题")]
    public sealed class UIThemeConfig : ScriptableObject
    {
        public const string ResourcesPath = "Configs/02_Feel/UIThemeConfig";

        public enum SafeAreaPolicy
        {
            [InspectorName("不处理")] None,
            [InspectorName("仅 HUD 层")] HudOnly,
            [InspectorName("仅弹窗层")] OverlayOnly,
            [InspectorName("HUD 与弹窗都处理")] HudAndOverlay,
        }

        // ==================== SafeArea ====================
        [Header("安全区（刘海/HomeIndicator 适配）")]
        [Tooltip("决定哪些 Canvas 层会按 Screen.safeArea 收缩 anchor,避免被刘海或底部指示条遮挡。")]
        [SerializeField] private SafeAreaPolicy _safeAreaPolicy = SafeAreaPolicy.HudAndOverlay;

        // ==================== 配色 ====================
        [Header("方块预览色")]
        [Tooltip("拖拽方块时,可放置位置的高亮颜色。")]
        [SerializeField] private Color _previewValidColor = new Color(1f, 1f, 1f, 0.4f);

        [Tooltip("拖拽方块时,不可放置位置的警示颜色。")]
        [SerializeField] private Color _previewInvalidColor = new Color(1f, 0.3f, 0.3f, 0.4f);

        [Tooltip("放置后会触发消除的整行/整列高亮颜色。")]
        [SerializeField] private Color _clearPreviewHighlightColor = new Color(1f, 1f, 1f, 0.6f);

        [Tooltip("空棋格的底色（透明度可为 0,表示完全透明）。")]
        [SerializeField] private Color _cellEmptyColor = new Color(0.2f, 0.2f, 0.25f, 0f);

        // ==================== 方块色板 ====================
        [Header("方块色板")]
        [Tooltip("方块可用颜色列表。从中随机抽色或按形状指定。")]
        [SerializeField] private Color[] _blockColors = new Color[]
        {
            new Color(0.95f, 0.30f, 0.30f, 1f),
            new Color(0.95f, 0.60f, 0.20f, 1f),
            new Color(0.95f, 0.90f, 0.25f, 1f),
            new Color(0.30f, 0.85f, 0.40f, 1f),
            new Color(0.30f, 0.55f, 0.95f, 1f),
            new Color(0.70f, 0.35f, 0.90f, 1f),
        };

        // ==================== 动效 ====================
        [Header("动效时长（秒）")]
        [Tooltip("分数跳动动效的总时长。")]
        [Range(0.05f, 1.5f)]
        [SerializeField] private float _scoreBounceDuration = 0.25f;

        [Tooltip("飘字默认存在时长。")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _floatingTextDefaultLifetime = 0.8f;

        // ==================== Public 访问器 ====================
        public SafeAreaPolicy SafeAreaMode => _safeAreaPolicy;
        public Color PreviewValidColor => _previewValidColor;
        public Color PreviewInvalidColor => _previewInvalidColor;
        public Color ClearPreviewHighlightColor => _clearPreviewHighlightColor;
        public Color CellEmptyColor => _cellEmptyColor;
        public Color[] BlockColors => _blockColors;
        public float ScoreBounceDuration => _scoreBounceDuration;
        public float FloatingTextDefaultLifetime => _floatingTextDefaultLifetime;
    }
}
