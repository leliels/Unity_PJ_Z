using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using BlockPuzzle.Core;

namespace BlockPuzzle.Config
{
    /// <summary>
    /// 飘字模板库:把"游戏事件"映射到"飘字怎么显示"。
    /// 美术想换图集 / 颜色 / 字体 / 飘动方向只改这里,不进代码。
    ///
    /// textFormat 占位符(运行时由 FloatingTextManager 替换):
    ///   {score}      ← 本次得分(放置/消除单条都有)
    ///   {multiplier} ← Combo 倍率
    ///   {lines}      ← 本次消除行数
    /// 保持 ASCII,便于 string.Format 解析。
    /// </summary>
    [CreateAssetMenu(fileName = "FloatingTextLibrary", menuName = "BlockPuzzle/游戏配置/飘字模板库")]
    public sealed class FloatingTextLibrary : ScriptableObject
    {
        public const string ResourcesPath = "Configs/02_Feel/FloatingTextLibrary";

        public enum RenderMode
        {
            [InspectorName("数字图集")] DigitAtlas,
            [InspectorName("TMP 字体")] TMPFont,
        }

        [Serializable]
        public class TemplateEntry
        {
            [Tooltip("模板内部名称,策划自定义,便于辨识。例如\"普通消除分\"\"超级 Combo\"。")]
            public string templateName = "新模板";

            [Tooltip("此模板用于哪个事件。同一事件可挂多条模板,触发时全部生效。")]
            public GameplayEventId eventId;

            [Header("渲染方式")]
            [Tooltip("用数字图集还是 TMP 文字。数字图集只能显示数字。")]
            public RenderMode renderMode = RenderMode.TMPFont;

            [Tooltip("数字图集(0~9 共 10 张 Sprite,索引 0=数字 0)。")]
            public Sprite[] digitSprites;

            [Tooltip("TMP 字体资产。")]
            public TMP_FontAsset tmpFont;

            [Tooltip("字号,像素。"), Range(20f, 200f)]
            public float fontSize = 60f;

            [Tooltip("起始颜色。")]
            public Color startColor = Color.white;

            [Tooltip("结束颜色(到飘字消失时)。")]
            public Color endColor = new Color(1f, 1f, 1f, 0f);

            [Header("文字格式")]
            [Tooltip("文字格式。占位符: {score}, {multiplier}, {lines}。例如 \"+{score}\" 或 \"Combo×{multiplier}\"。")]
            public string textFormat = "+{score}";

            [Header("动效")]
            [Tooltip("飘动方向(屏幕坐标系,(0,1) = 向上)。")]
            public Vector2 floatDir = new Vector2(0f, 1f);

            [Tooltip("飘动距离,屏幕像素。"), Range(0f, 600f)]
            public float floatDistance = 120f;

            [Tooltip("生命周期(秒)。"), Range(0.1f, 3f)]
            public float lifetime = 0.8f;

            [Tooltip("缩放曲线(横轴=时间归一)。1=正常大小,>1 放大,<1 缩小。")]
            public AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);

            [Tooltip("同时存在的最大数量,超出后旧的提前消失。"), Range(1, 30)]
            public int maxConcurrent = 8;
        }

        [Tooltip("飘字模板列表。")]
        [SerializeField] private List<TemplateEntry> _templates = new List<TemplateEntry>();

        public IReadOnlyList<TemplateEntry> Templates => _templates;

        public IEnumerable<TemplateEntry> GetTemplates(GameplayEventId id)
        {
            foreach (var t in _templates)
                if (t != null && t.eventId == id) yield return t;
        }
    }
}
