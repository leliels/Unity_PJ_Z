using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// 消除得分飘字管理器：
    /// 消除发生时逐条展示新版计分项（格子得分项、消除/Combo 得分项），
    /// 播完后触发总分跳动效果。
    /// </summary>
    public class FloatingScoreManager : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _canvasRect;

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

        private const int FallbackFontSize = 52;

        private static readonly Color CellScoreColor = Color.white;
        private static readonly Color ClearComboScoreColor = new Color(0.4f, 1f, 0.6f, 1f);

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
        public void SetFloatingScorePrefab(GameObject prefab)
        {
            if (_floatingScorePrefab == null)
                _floatingScorePrefab = prefab;
        }

        /// <summary>
        /// 添加格子得分项飘字。
        /// </summary>
        public void EnqueueCellScore(long score)
        {
            if (score <= 0) return;
            _pendingEntries.Enqueue(new FloatEntry
            {
                text = $"+{score}",
                color = CellScoreColor
            });
        }

        /// <summary>
        /// 添加消除/Combo 得分项飘字。
        /// </summary>
        public void EnqueueClearComboScore(int comboCount, long score)
        {
            if (score <= 0) return;
            string label = comboCount > 0 ? $"Combo ×{comboCount} +{score}" : $"+{score}";
            _pendingEntries.Enqueue(new FloatEntry
            {
                text = label,
                color = ClearComboScoreColor
            });
        }

        /// <summary>
        /// 开始播放所有待显示的飘字。
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

            yield return new WaitForSeconds(_floatDuration);

            _isPlaying = false;
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
                go = Instantiate(_floatingScorePrefab, _canvas.transform, false);
                go.name = "FloatingScore";
                rect = go.GetComponent<RectTransform>();
                if (rect == null) rect = go.AddComponent<RectTransform>();
                txt = go.GetComponent<Text>();
                if (txt == null) txt = go.AddComponent<Text>();
            }
            else
            {
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

            rect.anchorMin = _spawnAnchor;
            rect.anchorMax = _spawnAnchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, yOffset);

            txt.text = text;
            txt.color = color;

            StartCoroutine(AnimateFloat(rect, txt));
        }

        private IEnumerator AnimateFloat(RectTransform rect, Text txt)
        {
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, _floatDistance);
            Color startColor = txt.color;

            float popDuration = 0.15f;
            float elapsed = 0f;

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;
                float scale = Mathf.Lerp(0.5f, 1.2f, t);
                rect.localScale = Vector3.one * scale;
                yield return null;
            }

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

            elapsed = 0f;
            float fadeDuration = _floatDuration - 0.25f;
            yield return new WaitForSeconds(0.3f);

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

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
