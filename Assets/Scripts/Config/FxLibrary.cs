using System;
using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Core;

namespace BlockPuzzle.Config
{
    /// <summary>
    /// 特效与震屏库:把"游戏事件"映射到"要播的特效 Prefab + 震屏参数"。
    /// 美术新增/替换特效的入口 ——
    ///   1. Prefab 拖到 Assets/Prefabs/Fx/ 下
    ///   2. 打开此 .asset,加一项,选事件类型,把 Prefab 拖进 effectPrefab
    ///   3. 完成,代码不动
    ///
    /// 同一事件可以挂多条(比如消除既有粒子又有震屏),它们会同时触发。
    /// </summary>
    [CreateAssetMenu(fileName = "FxLibrary", menuName = "BlockPuzzle/游戏配置/特效与震屏库")]
    public sealed class FxLibrary : ScriptableObject
    {
        public const string ResourcesPath = "Configs/FxLibrary";

        public enum SpawnAnchor
        {
            [InspectorName("消除/放置点")] EventPosition,
            [InspectorName("棋盘中心")] BoardCenter,
            [InspectorName("屏幕中心")] ScreenCenter,
            [InspectorName("HUD 分数处")] HudScore,
        }

        [Serializable]
        public class FxEntry
        {
            [Tooltip("此条特效在哪个游戏事件触发。")]
            public GameplayEventId eventId;

            [Tooltip("要实例化的特效 Prefab(粒子/动画)。可为空,空时仅触发震屏。")]
            public GameObject effectPrefab;

            [Tooltip("特效生成的位置参考。事件位置 = 事件抛出时附带的屏幕坐标(消除中心、放置中心等)。")]
            public SpawnAnchor anchor = SpawnAnchor.EventPosition;

            [Tooltip("在锚点基础上的二维偏移(屏幕像素)。")]
            public Vector2 offset = Vector2.zero;

            [Tooltip("特效自动销毁前的存活秒数。"), Range(0f, 3f)]
            public float lifetime = 1f;

            [Header("震屏(可选)")]
            [Tooltip("震屏强度。0 = 不震;推荐 5~25。需要在玩法微调里启用震动总开关。"), Range(0f, 30f)]
            public float shakeAmplitude = 0f;

            [Tooltip("震屏时长(秒)。"), Range(0f, 1f)]
            public float shakeDuration = 0.2f;

            [Tooltip("震屏强度衰减曲线(横轴=时间归一,纵轴=强度系数 0~1)。")]
            public AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        }

        [Tooltip("特效配置列表。同一事件可以挂多条,会同时触发。")]
        [SerializeField] private List<FxEntry> _entries = new List<FxEntry>();

        public IReadOnlyList<FxEntry> Entries => _entries;

        public IEnumerable<FxEntry> GetEntries(GameplayEventId id)
        {
            foreach (var e in _entries)
                if (e != null && e.eventId == id) yield return e;
        }
    }
}
