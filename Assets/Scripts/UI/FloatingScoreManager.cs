using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Core;
using BlockPuzzle.Score;


namespace BlockPuzzle.UI
{
    /// <summary>
    /// 消除得分飘字管理器：
    /// 消除发生时逐条展示每项得分（放置分、消除基础加分、Combo 加成），
    /// 播完后触发总分跳动效果。
    ///
    /// 流程：
    /// ① 显示放置分飘字（如 "+4"）
    /// ② 显示消除基础加分飘字（如 "+12"）
    /// ③ 如果有 Combo → 显示 Combo 加成飘字（如 "Combo ×2 +240"）
    /// ④ 所有飘字展示完毕后 → 触发 OnAllFinished 回调 → 总分跳动

    /// </summary>
    public class FloatingScoreManager : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _canvasRect;

        // --- Prefab 和可配置参数（Inspector 可调） ---
        [Header("飘字 Prefab（可选，需含 Text + Outline 组件）")]
        [Tooltip("飘字 Prefab。为空时代码创建 fallback。可在 Prefab 中调整字体、字号、描边等。")]
        [SerializeField] private GameObject _floatingScorePrefab;

        [Header("飘字动画配置")]
        [Tooltip("飘字总持续时间（秒）")]
        [SerializeField] private float _floatDuration = 1.2f;
        [Tooltip("向上飘动像素距离")]
        [SerializeField] private float _floatDistance = 120f;
        [Tooltip("每条飘字之间的间隔（秒）")]
        [SerializeField] private float _staggerDelay = 0.25f;
        [Tooltip("飘字起始锚点位置（屏幕比例，0.65=偏上）")]
        [SerializeField] private Vector2 _spawnAnchor = new Vector2(0.5f, 0.65f);

        // fallback 字号（仅在无 Prefab 时使用）
        private const int FallbackFontSize = 52;

        // 颜色
        private static readonly Color PlacementColor = Color.white;
        private static readonly Color ClearColor = new Color(1f, 0.85f, 0.2f, 1f);  // 金色
        private static readonly Color ComboColor = new Color(0.4f, 1f, 0.6f, 1f);   // 绿色

        // 待显示的飘字队列
        private Queue<FloatEntry> _pendingEntries = new Queue<FloatEntry>();
        private bool _isPlaying;

        /// <summary>所有飘字播放完毕事件</summary>
        public event Action OnAllFinished;

        private struct FloatEntry
        {
            public string text;
            public Color color;
        }

        /// <summary>
        /// 初始化，绑定到指定 Canvas
        /// </summary>
        public void Init(Canvas canvas)
        {
            _canvas = canvas;
            _canvasRect = canvas.GetComponent<RectTransform>();
        }

        /// <summary>外部设置飘字 Prefab</summary>
        public void SetFloatingScorePrefab(GameObject prefab) { if (_floatingScorePrefab == null) _floatingScorePrefab = prefab; }

        /// <summary>
        /// 添加放置分飘字。
        /// </summary>
        public void EnqueuePlacementScore(int placementScore)
        {
            if (placementScore <= 0) return;
            _pendingEntries.Enqueue(new FloatEntry
            {
                text = $"+{placementScore}",
                color = PlacementColor
            });
        }


        /// <summary>
        /// 添加消除分飘字（单行/列的分数）
        /// </summary>
        public void EnqueueClearScore(long score, int lineCount)
        {
            if (score <= 0) return;
            string label = lineCount > 1 ? $"×{lineCount} +{score}" : $"+{score}";
            _pendingEntries.Enqueue(new FloatEntry
            {
                text = label,
                color = ClearColor
            });
        }

        /// <summary>
        /// 添加 Combo 加成飘字。
        /// </summary>
        public void EnqueueComboBonus(int comboCount, long bonusScore)
        {
            if (comboCount <= 0 || bonusScore <= 0) return;
            _pendingEntries.Enqueue(new FloatEntry
            {
                text = $"Combo ×{comboCount} +{bonusScore}",
                color = ComboColor
            });
        }


        /// <summary>
        /// 开始播放所有待显示的飘字
        /// </summary>
        public void PlayAll()
        {
            if (_pendingEntries.Count == 0 || _isPlaying) return;
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            _isPlaying = true;
            float yOffset = 0f;

            while (_pendingEntries.Count > 0)
            {
                var entry = _pendingEntries.Dequeue();
                SpawnFloatingText(entry.text, entry.color, yOffset);
                yOffset += 60f;
                yield return new WaitForSeconds(_staggerDelay);
            }

            // 等待最后一条飘字的动画播完（上飘 + 淡出）
            yield return new WaitForSeconds(_floatDuration);

            _isPlaying = false;

            // 通知所有飘字已播放完毕 → 触发总分跳动
            OnAllFinished?.Invoke();
        }

        private void SpawnFloatingText(string text, Color color, float yOffset)
        {
            if (_canvas == null) return;

            GameObject go;
            RectTransform rect;
            Text txt;

            if (_floatingScorePrefab != null)
            {
                // Prefab 方式：字体、字号、描边等由 Prefab 决定
                go = Instantiate(_floatingScorePrefab, _canvas.transform, false);
                go.name = "FloatingScore";
                rect = go.GetComponent<RectTransform>();
                if (rect == null) rect = go.AddComponent<RectTransform>();
                txt = go.GetComponent<Text>();
                if (txt == null) txt = go.AddComponent<Text>();
            }
            else
            {
                // Fallback：代码创建
                go = new GameObject("FloatingScore");
                go.transform.SetParent(_canvas.transform, false);
                rect = go.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(600, 80);

                txt = go.AddComponent<Text>();
                txt.fontSize = FallbackFontSize;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (txt.font == null)
                    txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow = VerticalWrapMode.Overflow;

                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, 0.8f);
                outline.effectDistance = new Vector2(2, -2);
            }

            // 设置位置（使用可配置的锚点）
            rect.anchorMin = _spawnAnchor;
            rect.anchorMax = _spawnAnchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, yOffset);

            // 设置文字和颜色（无论 Prefab 还是 fallback 都由代码控制）
            txt.text = text;
            txt.color = color;

            StartCoroutine(AnimateFloat(rect, txt));
        }

        private IEnumerator AnimateFloat(RectTransform rect, Text txt)
        {
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, _floatDistance);
            Color startColor = txt.color;

            // 弹出动画：从 0.5 倍缩放到 1.2 倍再回到 1.0 倍
            float popDuration = 0.15f;
            float elapsed = 0f;

            // 弹出阶段
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;
                float scale = Mathf.Lerp(0.5f, 1.2f, t);
                rect.localScale = Vector3.one * scale;
                yield return null;
            }

            // 回弹
            elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.1f;
                float scale = Mathf.Lerp(1.2f, 1f, t);
                rect.localScale = Vector3.one * scale;
                yield return null;
            }
            rect.localScale = Vector3.one;

            // 上飘 + 淡出阶段
            elapsed = 0f;
            float fadeDuration = _floatDuration - 0.25f;
            // 先停留一会
            yield return new WaitForSeconds(0.3f);

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                // 上飘
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

                // 后半段淡出
                if (t > 0.5f)
                {
                    float fadeT = (t - 0.5f) / 0.5f;
                    txt.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeT);
                }

                yield return null;
            }

            Destroy(rect.gameObject);
        }
    }
}
