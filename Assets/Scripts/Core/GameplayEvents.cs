using System;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// 游戏内核心事件类型。
    /// 这是"美术/策划想给某个时机加特效/音效/飘字时,只看这张表就知道有哪些时机"的契约表。
    /// 各 Manager 在合适时机抛出 <see cref="GameplayEvents.Raise"/>,
    /// FxManager / FloatingTextManager / AudioBindings / FeedbackManager 监听后按 SO 配置驱动表现。
    ///
    /// 新增事件时,务必同时在 9-术语表文档中补充中文说明,并在 InspectorName 上加中文标签。
    /// </summary>
    public enum GameplayEventId
    {
        /// <summary>方块放置成功（无论是否触发消除）</summary>
        BlockPlaced,
        /// <summary>方块消除（一行/一列被清空）</summary>
        LineCleared,
        /// <summary>Combo 触发（连击数增长）</summary>
        ComboTriggered,
        /// <summary>Combo 中断（CCD 归零、Combo 重置）</summary>
        ComboBroken,
        /// <summary>新最高分</summary>
        NewHighScore,
        /// <summary>一局开始</summary>
        GameStarted,
        /// <summary>一局结束（无可放置方块）</summary>
        GameOver,
        /// <summary>UI 按钮点击</summary>
        ButtonClicked,
        /// <summary>低候选警告：剩余可放置方块数低于阈值</summary>
        LowOptionWarning,
    }

    /// <summary>
    /// 一次事件的上下文数据。
    /// 用单一可空字段持有不同事件需要的额外信息,避免事件爆炸式增加重载。
    /// 字段命名贴近事件需要,SO 中按 GameplayEventId 派发即可。
    /// </summary>
    public struct GameplayEventArgs
    {
        /// <summary>事件发生时的世界/屏幕参考位置（屏幕像素坐标,可空）。
        /// 例如消除点用消除中心,放置点用放置时方块中心。</summary>
        public UnityEngine.Vector2? ScreenPosition;
        /// <summary>事件涉及的方块行列(可空)。</summary>
        public UnityEngine.Vector2Int? BoardCell;
        /// <summary>本次事件相关数值,如本次得分、Combo 倍率、消除行数等。</summary>
        public int IntValue;
        /// <summary>本次事件相关数值的辅助字段。</summary>
        public int IntValue2;

        public static GameplayEventArgs Empty => default;

        public static GameplayEventArgs WithScore(int score) => new GameplayEventArgs { IntValue = score };

        public static GameplayEventArgs WithLineClear(int lineCount, int comboCount, UnityEngine.Vector2? screenPos = null)
            => new GameplayEventArgs { IntValue = lineCount, IntValue2 = comboCount, ScreenPosition = screenPos };
    }

    /// <summary>
    /// 全局事件总线。各 Manager 调用 Raise,各表现层调用 Subscribe/Unsubscribe。
    /// 静态类避免单例创建顺序问题；监听方在 OnEnable / OnDisable 中订阅/取消即可。
    /// </summary>
    public static class GameplayEvents
    {
        public static event Action<GameplayEventId, GameplayEventArgs> OnEvent;

        public static void Raise(GameplayEventId id) => OnEvent?.Invoke(id, GameplayEventArgs.Empty);

        public static void Raise(GameplayEventId id, GameplayEventArgs args) => OnEvent?.Invoke(id, args);

        /// <summary>仅供单元测试 / 场景重载时手动清空所有订阅,生产代码勿用。</summary>
        public static void ClearAllSubscribers() => OnEvent = null;
    }
}
