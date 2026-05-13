using UnityEngine;
using BlockPuzzle.Audio;
using BlockPuzzle.Block;
using BlockPuzzle.Mode;
using BlockPuzzle.Score;

namespace BlockPuzzle.Config
{
    /// <summary>
    /// 游戏总配置 (GameConfig)。
    /// 是 SceneBootstrap 唯一直接读取的入口。所有其它 SO 通过这里串起来。
    ///
    /// 美术/策划日常工作流：菜单 BlockPuzzle/游戏配置中心 → 自动选中本资产
    /// → Inspector 顶部看使用说明 → 横排子配置中点击"打开"按钮直达对应 SO。
    ///
    /// 新增子配置时,在本文件加字段 + 在 GameConfigInspector 的子配置列表里加一项即可。
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "BlockPuzzle/游戏配置/总配置 (GameConfig)")]
    public sealed class GameConfig : ScriptableObject
    {
        public const string ResourcesPath = "Configs/GameConfig";

        [Header("核心玩法配置")]
        [Tooltip("计分公式:基础分、Combo、CCD、排分倍率表。")]
        [SerializeField] private ScoreConfig _score;

        [Tooltip("方块形状库:形状、权重、4 方向变体。改这里 = 改方块池。")]
        [SerializeField] private BlockShapeDatabase _shapes;

        [Tooltip("布局参数:棋盘比例、留白、候选区缩放。所有屏幕适配靠这一份。")]
        [SerializeField] private LayoutConfig _layout;

        [Tooltip("UI 主题:配色、SafeArea 策略、动效时长。")]
        [SerializeField] private UIThemeConfig _theme;

        [Tooltip("玩法微调:震动/音效总开关、提示阈值等小杂项。")]
        [SerializeField] private GameplayTuning _gameplay;

        [Header("模式与音效")]
        [Tooltip("模式目录:Title 界面会从这里读出模式列表。")]
        [SerializeField] private ModeCatalog _modeCatalog;

        [Tooltip("默认模式:首次进入或没有指定模式时使用。")]
        [SerializeField] private GameModeConfig _defaultMode;

        [Tooltip("音效素材库:所有 AudioCue 集中在这里。")]
        [SerializeField] private AudioLibrary _audioLibrary;

        // 注:FxLibrary / FloatingTextLibrary / AudioBindings 三个自助体系 SO 在 M-R5 阶段
        // 引入。届时直接在本文件追加字段即可,Unity 反序列化对新增字段使用默认值,与现有
        // GameConfig.asset 完全兼容。

        // ==================== Public 访问器 ====================
        public ScoreConfig Score => _score;
        public BlockShapeDatabase Shapes => _shapes;
        public LayoutConfig Layout => _layout;
        public UIThemeConfig Theme => _theme;
        public GameplayTuning Gameplay => _gameplay;
        public ModeCatalog ModeCatalog => _modeCatalog;
        public GameModeConfig DefaultMode => _defaultMode;
        public AudioLibrary AudioLibrary => _audioLibrary;

        /// <summary>
        /// 运行时安全加载入口。优先从 Resources/Configs/GameConfig 加载;
        /// 找不到时返回 null,调用方应自行回退到旧行为(向后兼容 M-R1 之前的工程)。
        /// </summary>
        public static GameConfig LoadFromResources()
        {
            return Resources.Load<GameConfig>(ResourcesPath);
        }

        /// <summary>
        /// 运行时校验:返回当前缺失的关键字段名列表。空数组 = 全部齐备。
        /// </summary>
        public string[] ValidateRuntime()
        {
            var missing = new System.Collections.Generic.List<string>();
            if (_score == null) missing.Add("计分配置 (Score)");
            if (_shapes == null) missing.Add("方块形状库 (Shapes)");
            if (_layout == null) missing.Add("布局配置 (Layout)");
            if (_theme == null) missing.Add("UI 主题 (Theme)");
            // M-R5 之前的字段允许为空,不校验
            return missing.ToArray();
        }
    }
}
