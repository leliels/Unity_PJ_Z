using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Core;
using BlockPuzzle.Score;
using BlockPuzzle.Utils;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// 消除得分飘字管理器：
    /// 消除发生时逐条展示每项得分（放置分、消除分、Combo 加成），
    /// 播完后汇入总分并触发总分跳动效果。
    /// </summary>
    public class FloatingScoreManager : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _canvasRect;

        // 飘字配置
        private const float FloatDuration = 1.2f;   // 飘字总持续时间
        private const float FloatDistance = 120f;    // 向上飘动像素距离
        private const float StaggerDelay = 0.25f;   // 每条飘字之间的间隔
        private const int FloatFontSize = 52;        // 飘字字号

        // 颜色
        private static readonly Color PlacementColor = Color.white;
        private static readonly Color ClearColor = new Color(1f, 0.85f, 0.2f, 1f);  // 金色
        private static readonly Color ComboColor = new Color(0.4f, 1f, 0.6f, 1f);   // 绿色

        // 待显示的飘字队列
        private Queue<FloatEntry> _pendingEntries = new Queue<FloatEntry>();
        private bool _isPlaying;

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

        /// <summary>
        /// 添加放置分飘字
        /// </summary>
        public void EnqueuePlacementScore(int cellCount)
        {
            int score = cellCount * Constants.ScorePerCell;
            if (score <= 0) return;
            _pendingEntries.Enqueue(new FloatEntry
            {
                text = $"+{score}",
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
        /// 添加 Combo 加成飘字
        /// </summary>
        public void EnqueueComboBonus(int comboCount, float multiplier, long bonusScore)
        {
            if (bonusScore <= 0) return;
            _pendingEntries.Enqueue(new FloatEntry
            {
                text = $"Combo ×{comboCount} (+{bonusScore})",
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
                yOffset += 60f; // 每条飘字错开一点高度
                yield return new WaitForSeconds(StaggerDelay);
            }

            _isPlaying = false;
        }

        private void SpawnFloatingText(string text, Color color, float yOffset)
        {
            if (_canvas == null) return;

            var go = new GameObject("FloatingScore");
            go.transform.SetParent(_canvas.transform, false);

            var rect = go.AddComponent<RectTransform>();
            // 在屏幕中上方（棋盘上方区域）生成
            rect.anchorMin = new Vector2(0.5f, 0.65f);
            rect.anchorMax = new Vector2(0.5f, 0.65f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, yOffset);
            rect.sizeDelta = new Vector2(600, 80);

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = FloatFontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null)
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            // 添加描边使飘字更醒目
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(2, -2);

            StartCoroutine(AnimateFloat(rect, txt));
        }

        private IEnumerator AnimateFloat(RectTransform rect, Text txt)
        {
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, FloatDistance);
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
            float fadeDuration = FloatDuration - 0.25f;
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
