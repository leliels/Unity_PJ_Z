using System;
using System.Collections;
using UnityEngine;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// 组合飘字根组件：管理 Combo 模块、格子加成分模块、消除Combo分模块的显示/隐藏和整体动画。
    /// Prefab 结构：
    ///   Root (ScorePopupView + CanvasGroup + RectTransform)
    ///     ├── ComboModule (ComboFloatingScoreView) — 显示 "Combo ×N"
    ///     ├── CellScoreModule (FloatingScoreTextModule) — 显示 "+cellScore"
    ///     └── ClearScoreModule (FloatingScoreTextModule) — 显示 "+clearComboScore"
    /// 排布顺序：Combo 在最前，格子分与消除分排成一排。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScorePopupView : MonoBehaviour
    {
        [Header("子模块引用")]
        [SerializeField] private ComboFloatingScoreView _comboModule;
        [SerializeField] private FloatingScoreTextModule _cellScoreModule;
        [SerializeField] private FloatingScoreTextModule _clearScoreModule;

        [Header("动画配置")]
        [Tooltip("弹出阶段时长（秒）")]
        [SerializeField] private float _popDuration = 0.18f;
        [Tooltip("弹出峰值缩放")]
        [SerializeField] private float _popScale = 1.15f;
        [Tooltip("停留时长（秒）")]
        [SerializeField] private float _holdDuration = 0.4f;
        [Tooltip("上浮+淡出时长（秒）")]
        [SerializeField] private float _fadeDuration = 0.7f;
        [Tooltip("上浮距离（像素）")]
        [SerializeField] private float _floatDistance = 100f;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        /// <summary>播放完毕回调</summary>
        public event Action OnFinished;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 设置消除场景下的显示数据（有 Combo 的完整消除飘字）。
        /// </summary>
        /// <param name="comboCount">Combo 倍率（≤1 时隐藏 Combo 模块）</param>
        /// <param name="cellScore">格子加成分（≤0 时隐藏格子分模块）</param>
        /// <param name="clearComboScore">消除Combo分</param>
        public void SetClearScoreData(int comboCount, long cellScore, long clearComboScore)
        {
            // Combo 模块
            if (_comboModule != null)
            {
                if (comboCount > 1)
                {
                    _comboModule.gameObject.SetActive(true);
                    _comboModule.SetCombo(comboCount);
                }
                else
                {
                    _comboModule.gameObject.SetActive(false);
                }
            }

            // 格子加成分模块
            if (_cellScoreModule != null)
            {
                if (cellScore > 0)
                {
                    _cellScoreModule.gameObject.SetActive(true);
                    _cellScoreModule.SetScore(cellScore);
                }
                else
                {
                    _cellScoreModule.gameObject.SetActive(false);
                }
            }

            // 消除Combo分模块
            if (_clearScoreModule != null)
            {
                _clearScoreModule.gameObject.SetActive(true);
                _clearScoreModule.SetScore(clearComboScore);
            }
        }

        /// <summary>
        /// 设置非消除放置分数据（仅显示放置分，隐藏 Combo 和消除分模块）。
        /// </summary>
        public void SetPlaceScoreData(long placeScore)
        {
            if (_comboModule != null)
                _comboModule.gameObject.SetActive(false);

            if (_cellScoreModule != null)
            {
                _cellScoreModule.gameObject.SetActive(true);
                _cellScoreModule.SetScore(placeScore);
            }

            if (_clearScoreModule != null)
                _clearScoreModule.gameObject.SetActive(false);
        }

        /// <summary>
        /// 播放弹出 + 上浮 + 淡出动画，结束后自动销毁。
        /// </summary>
        public void PlayAnimation()
        {
            StartCoroutine(AnimationCoroutine());
        }

        private IEnumerator AnimationCoroutine()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();

            _canvasGroup.alpha = 1f;
            Vector2 startPos = _rectTransform.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, _floatDistance);

            // === Phase 1: 弹出（从 0.75 弹到 popScale） ===
            float elapsed = 0f;
            float halfPop = _popDuration * 0.6f;
            while (elapsed < halfPop)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfPop);
                float scale = Mathf.Lerp(0.75f, _popScale, t);
                _rectTransform.localScale = Vector3.one * scale;
                yield return null;
            }

            // === Phase 2: 回弹到 1.0 ===
            elapsed = 0f;
            float bounceback = _popDuration * 0.4f;
            while (elapsed < bounceback)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / bounceback);
                float scale = Mathf.Lerp(_popScale, 1f, t);
                _rectTransform.localScale = Vector3.one * scale;
                yield return null;
            }
            _rectTransform.localScale = Vector3.one;

            // === Phase 3: 停留 ===
            yield return new WaitForSeconds(_holdDuration);

            // === Phase 4: 上浮 + 淡出 ===
            elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeDuration);

                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

                // 后半段淡出
                if (t > 0.4f)
                {
                    float fadeT = (t - 0.4f) / 0.6f;
                    _canvasGroup.alpha = 1f - fadeT;
                }

                yield return null;
            }

            _canvasGroup.alpha = 0f;
            OnFinished?.Invoke();
            Destroy(gameObject);
        }
    }
}
