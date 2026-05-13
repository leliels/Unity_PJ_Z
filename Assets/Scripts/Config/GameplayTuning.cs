using UnityEngine;

namespace BlockPuzzle.Config
{
    /// <summary>
    /// 玩法微调：连击窗口、震动开关、提示阈值等"行为层"参数。
    /// 这里放的是不属于计分/形状/音效/特效任一专门 SO,但又需要策划调整的小杂项。
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayTuning", menuName = "BlockPuzzle/游戏配置/玩法微调")]
    public sealed class GameplayTuning : ScriptableObject
    {
        public const string ResourcesPath = "Configs/GameplayTuning";

        [Header("反馈开关")]
        [Tooltip("总开关:是否启用震动/抖动反馈。关闭后所有 FxLibrary 中配置的震屏都不生效。")]
        [SerializeField] private bool _enableHapticFeedback = true;

        [Tooltip("总开关:是否启用音效。关闭后所有 AudioBindings 中的音效都不生效。")]
        [SerializeField] private bool _enableSfx = true;

        [Tooltip("总开关:是否启用 BGM。")]
        [SerializeField] private bool _enableBgm = true;

        [Header("提示与警告")]
        [Tooltip("当所有候选方块都无法放置时,延迟多少秒后触发 GameOver。0 = 立即触发。")]
        [Range(0f, 3f)]
        [SerializeField] private float _gameOverDelay = 0.5f;

        [Tooltip("剩余可放置候选方块数 ≤ 此值时,触发\"无可放置\"警告事件。0 = 不警告。")]
        [Range(0, 3)]
        [SerializeField] private int _lowOptionWarningThreshold = 1;

        [Header("拖拽手感")]
        [Tooltip("拖拽时方块相对手指的 X 偏移(单位 = 棋盘格大小的倍数)。0 = 不偏移。")]
        [Range(-3f, 3f)]
        [SerializeField] private float _dragOffsetXInCells = 0f;

        [Tooltip("拖拽时方块相对手指的 Y 偏移(单位 = 棋盘格大小的倍数)。" +
                 "正数 = 方块抬到手指上方,避免被手指遮挡;手机推荐 1.5~2.5,鼠标推荐 0~0.5。")]
        [Range(-3f, 5f)]
        [SerializeField] private float _dragOffsetYInCells = 1.5f;

        [Tooltip("吸附宽容度(0~1)。0 = 严格按方块中心格判定;0.5 = 手指偏离半格内仍按当前位置吸附。" +
                 "目前未使用,M-R8+ 接入。")]
        [Range(0f, 1f)]
        [SerializeField] private float _snapTolerance = 0f;

        // ==================== Public 访问器 ====================
        public bool EnableHapticFeedback => _enableHapticFeedback;
        public bool EnableSfx => _enableSfx;
        public bool EnableBgm => _enableBgm;
        public float GameOverDelay => _gameOverDelay;
        public int LowOptionWarningThreshold => _lowOptionWarningThreshold;
        public float DragOffsetXInCells => _dragOffsetXInCells;
        public float DragOffsetYInCells => _dragOffsetYInCells;
        public float SnapTolerance => _snapTolerance;
    }
}
