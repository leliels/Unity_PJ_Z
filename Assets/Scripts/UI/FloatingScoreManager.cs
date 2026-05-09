using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// 消除得分飘字管理器（重构版）：
    /// 支持结构化飘字（ScorePopupView Prefab）和旧版纯文本 fallback。
    /// 消除时展示 Combo + 格子加成分 + 消除Combo分 三模块组合；
    /// 非消除放置时展示单独放置分飘字。
    /// 播完后触发总分跳动效果。
    /// </summary>
    public class FloatingScoreManager : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _canvasRect;

        [Header("新版组合飘字 Prefab（需含 ScorePopupView 组件）")]
        [Tooltip("新版组合飘字 Prefab。为空时使用旧版纯文本 fallback。")]
        [SerializeField] private GameObject _scorePopupPrefab;

        [Header("旧版飘字 Prefab（fallback，需含 Text + Outline 组件）")]
        [Tooltip("旧版飘字 Prefab。新版 Prefab 未配置时使用此 fallback。")]
        [SerializeField] private GameObject _floatingScorePrefab;

        [Header("飘字动画配置（旧版 fallback 用）")]
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
        private static readonly Color PlaceScoreColor = new Color(1f, 0.95f, 0.7f, 1f);

        /// <summary>所有飘字播放完毕事件</summary>
        public event Action OnAllFinished;

        // ==================== 结构化条目 ====================

        private enum PopupType { ClearScore, PlaceScore }

        private struct PopupEntry
        {
            public PopupType type;
            // 消除场景
            public int comboCount;
            public long cellScore;
            public long clearComboScore;
            // 放置场景
            public long placeScore;
        }

        private Queue<PopupEntry> _pendingEntries = new Queue<PopupEntry>();
        private bool _isPlaying;

        // ==================== 旧版 fallback 队列 ====================

        private struct FloatEntry
        {
            public string text;
            public Color color;
        }
        private Queue<FloatEntry> _pendingLegacyEntries = new Queue<FloatEntry>();

        // ==================== 初始化 ====================

        /// <summary>
        /// 初始化，绑定到指定 Canvas
        /// </summary>
        public void Init(Canvas canvas)
        {
            _canvas = canvas;
            _canvasRect = canvas.GetComponent<RectTransform>();
        }

        /// <summary>外部设置旧版飘字 Prefab（兼容 SceneBootstrap 旧调用）</summary>
        public void SetFloatingScorePrefab(GameObject prefab)
        {
            if (_floatingScorePrefab == null)
                _floatingScorePrefab = prefab;
        }

        /// <summary>外部设置新版组合飘字 Prefab</summary>
        public void SetScorePopupPrefab(GameObject prefab)
        {
            if (_scorePopupPrefab == null)
                _scorePopupPrefab = prefab;
        }

        // ==================== 新版结构化入队 ====================

        /// <summary>
        /// 消除得分：将 Combo + 格子加成分 + 消除Combo分 作为一个组合飘字条目入队。
        /// </summary>
        public void EnqueueClearScore(int comboCount, long cellScore, long clearComboScore)
        {
            _pendingEntries.Enqueue(new PopupEntry
            {
                type = PopupType.ClearScore,
                comboCount = comboCount,
                cellScore = cellScore,
                clearComboScore = clearComboScore
            });
        }

        /// <summary>
        /// 非消除放置得分：单独放置分飘字入队。
        /// </summary>
        public void EnqueuePlaceScore(long placeScore)
        {
            if (placeScore <= 0) return;
            _pendingEntries.Enqueue(new PopupEntry
            {
                type = PopupType.PlaceScore,
                placeScore = placeScore
            });
        }

        // ==================== 旧版兼容入队（保留以支持未绑定新 Prefab 时的 fallback） ====================

        /// <summary>
        /// [旧版兼容] 添加格子得分项飘字。
        /// </summary>
        public void EnqueueCellScore(long score)
        {
            if (score <= 0) return;
            _pendingLegacyEntries.Enqueue(new FloatEntry
            {
                text = $"+{score}",
                color = CellScoreColor
            });
        }

        /// <summary>
        /// [旧版兼容] 添加消除/Combo 得分项飘字。
        /// </summary>
        public void EnqueueClearComboScore(int comboCount, long score)
        {
            if (score <= 0) return;
            string label = comboCount > 1 ? $"Combo ×{comboCount} +{score}" : $"+{score}";
            _pendingLegacyEntries.Enqueue(new FloatEntry
            {
                text = label,
                color = ClearComboScoreColor
            });
        }

        // ==================== 播放 ====================

        /// <summary>
        /// 开始播放所有待显示的飘字。
        /// </summary>
        public void PlayAll()
        {
            if (_isPlaying) return;

            // 优先使用新版结构化队列
            if (_pendingEntries.Count > 0)
            {
                StartCoroutine(PlayNewSequence());
            }
            else if (_pendingLegacyEntries.Count > 0)
            {
                StartCoroutine(PlayLegacySequence());
            }
        }

        // ==================== 新版播放逻辑 ====================

        private IEnumerator PlayNewSequence()
        {
            _isPlaying = true;
            int spawnedCount = 0;
            int finishedCount = 0;

            while (_pendingEntries.Count > 0)
            {
                var entry = _pendingEntries.Dequeue();
                spawnedCount++;

                if (_scorePopupPrefab != null)
                {
                    SpawnScorePopup(entry, () => { finishedCount++; });
                }
                else
                {
                    // Fallback：用旧版方式显示
                    SpawnLegacyFromEntry(entry);
                }

                if (_pendingEntries.Count > 0)
                    yield return new WaitForSeconds(_staggerDelay);
            }

            if (_scorePopupPrefab != null)
            {
                // 等待所有 popup 动画完成
                while (finishedCount < spawnedCount)
                    yield return null;
            }
            else
            {
                // 旧版等待固定时间
                yield return new WaitForSeconds(_floatDuration);
            }

            _isPlaying = false;
            OnAllFinished?.Invoke();
        }

        private void SpawnScorePopup(PopupEntry entry, Action onFinished)
        {
            if (_canvas == null) return;

            var go = Instantiate(_scorePopupPrefab, _canvas.transform, false);
            go.name = "ScorePopup";

            var rect = go.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = _spawnAnchor;
                rect.anchorMax = _spawnAnchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
            }

            var view = go.GetComponent<ScorePopupView>();
            if (view != null)
            {
                switch (entry.type)
                {
                    case PopupType.ClearScore:
                        view.SetClearScoreData(entry.comboCount, entry.cellScore, entry.clearComboScore);
                        break;
                    case PopupType.PlaceScore:
                        view.SetPlaceScoreData(entry.placeScore);
                        break;
                }

                view.OnFinished += onFinished;
                view.PlayAnimation();
            }
            else
            {
                // Prefab 没挂 ScorePopupView，fallback 销毁
                Debug.LogWarning("[FloatingScoreManager] ScorePopup Prefab 未挂载 ScorePopupView 组件");
                Destroy(go);
                onFinished?.Invoke();
            }
        }

        private void SpawnLegacyFromEntry(PopupEntry entry)
        {
            switch (entry.type)
            {
                case PopupType.ClearScore:
                    if (entry.cellScore > 0)
                        SpawnFloatingText($"+{entry.cellScore}", CellScoreColor, 0f);
                    string comboLabel = entry.comboCount > 1
                        ? $"Combo ×{entry.comboCount} +{entry.clearComboScore}"
                        : $"+{entry.clearComboScore}";
                    SpawnFloatingText(comboLabel, ClearComboScoreColor, 60f);
                    break;
                case PopupType.PlaceScore:
                    SpawnFloatingText($"+{entry.placeScore}", PlaceScoreColor, 0f);
                    break;
            }
        }

        // ==================== 旧版播放逻辑 ====================

        private IEnumerator PlayLegacySequence()
        {
            _isPlaying = true;
            float yOffset = 0f;

            while (_pendingLegacyEntries.Count > 0)
            {
                var entry = _pendingLegacyEntries.Dequeue();
                SpawnFloatingText(entry.text, entry.color, yOffset);
                yOffset += 60f;
                yield return new WaitForSeconds(_staggerDelay);
            }

            yield return new WaitForSeconds(_floatDuration);

            _isPlaying = false;
            OnAllFinished?.Invoke();
        }

        // ==================== 旧版飘字生成 ====================

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
