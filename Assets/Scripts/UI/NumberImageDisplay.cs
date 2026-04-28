using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// 用数字图片精灵显示数值的 UI 组件
    /// 支持分数变化时的跳动动画效果
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class NumberImageDisplay : MonoBehaviour
    {
        public enum Alignment { Left, Center, Right }

        [Header("数字图片 (0-9)")]
        [SerializeField] private Sprite[] _numberSprites = new Sprite[10];

        [Header("布局")]
        [SerializeField] private float _digitWidth = 50f;
        [SerializeField] private float _digitHeight = 70f;
        [SerializeField] private float _spacing = 5f;
        [SerializeField] private Alignment _alignment = Alignment.Center;

        [Header("跳动动画（飘字播放完毕后触发）")]
        [Tooltip("跳动缩放峰值（1.0=不跳，1.3=放大30%）")]
        [SerializeField] private float _bounceScale = 1.3f;
        [Tooltip("跳动动画时长（秒）")]
        [SerializeField] private float _bounceDuration = 0.35f;

        private readonly Image[] _digitImages = new Image[10];
        private RectTransform _rectTransform;
        private int _lastValue = -1;
        private int _currentDigitCount;

        public bool HasValidSprites => _numberSprites != null && _numberSprites.Length >= 10 && _numberSprites[0] != null;

        public float DigitWidth
        {
            get => _digitWidth;
            set { _digitWidth = value; ForceRefresh(_lastValue); }
        }

        public float DigitHeight
        {
            get => _digitHeight;
            set { _digitHeight = value; ForceRefresh(_lastValue); }
        }

        public float Spacing
        {
            get => _spacing;
            set { _spacing = value; ForceRefresh(_lastValue); }
        }

        public Alignment TextAlignment
        {
            get => _alignment;
            set { _alignment = value; ForceRefresh(_lastValue); }
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            EnsureDigitCount(0);
        }

        /// <summary>设置要显示的数字</summary>
        public void SetNumber(int value)
        {
            if (_lastValue == value && _currentDigitCount > 0) return;
            _lastValue = value;
            RefreshDisplay();
        }

        /// <summary>强制刷新显示（OnValidate 热更新用）</summary>
        public void ForceRefresh(int value)
        {
            _lastValue = value;
            if (_digitImages[0] != null)
                RefreshDisplay();
        }

        /// <summary>
        /// 播放分数跳动动画（飘字全部播完后调用）
        /// 设计文档 131 行：总分数字有跳动效果
        /// </summary>
        public void PlayBounceEffect()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            StopAllCoroutines();
            StartCoroutine(BounceCoroutine());
        }

        private IEnumerator BounceCoroutine()
        {
            // 放大到峰值
            float half = _bounceDuration * 0.5f;
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                // ease-out quad
                float s = 1f + (_bounceScale - 1f) * (1f - (1f - t) * (1f - t));
                _rectTransform.localScale = Vector3.one * s;
                yield return null;
            }
            // 回弹到正常大小
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                // ease-out quad
                float s = _bounceScale - (_bounceScale - 1f) * (1f - (1f - t) * (1f - t));
                _rectTransform.localScale = Vector3.one * s;
                yield return null;
            }
            _rectTransform.localScale = Vector3.one;
        }

        private void RefreshDisplay()
        {
            if (_numberSprites == null || _numberSprites.Length < 10) return;

            string numStr = _lastValue.ToString();
            int needed = numStr.Length;
            EnsureDigitCount(needed);
            _currentDigitCount = needed;

            Vector2 uniformSize = new Vector2(_digitWidth, _digitHeight);

            for (int i = 0; i < needed; i++)
            {
                int digit = numStr[i] - '0';
                var img = _digitImages[i];
                img.gameObject.SetActive(true);

                if (digit >= 0 && digit <= 9 && _numberSprites[digit] != null)
                {
                    img.sprite = _numberSprites[digit];
                    img.preserveAspect = false;
                    img.rectTransform.sizeDelta = uniformSize;
                }
            }

            for (int i = needed; i < _digitImages.Length; i++)
            {
                if (_digitImages[i] != null)
                    _digitImages[i].gameObject.SetActive(false);
            }

            LayoutDigits(needed);
        }

        private void EnsureDigitCount(int count)
        {
            Transform t = transform;
            Vector2 uniformSize = new Vector2(_digitWidth, _digitHeight);

            for (int i = 0; i < count; i++)
            {
                if (_digitImages[i] != null) continue;

                var child = new GameObject($"Digit_{i}", typeof(RectTransform), typeof(Image));
                child.transform.SetParent(t, false);
                var img = child.GetComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = false;
                img.rectTransform.sizeDelta = uniformSize;
                _digitImages[i] = img;
            }
        }

        private void LayoutDigits(int count)
        {
            if (count <= 0) return;

            float totalWidth = count * _digitWidth + (count - 1) * _spacing;
            float startX = _alignment switch
            {
                Alignment.Center => -totalWidth / 2f,
                Alignment.Right => -totalWidth,
                _ => 0f // Left
            };

            for (int i = 0; i < count; i++)
            {
                float x = startX + i * (_digitWidth + _spacing);
                _digitImages[i].rectTransform.anchoredPosition = new Vector2(x, 0f);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying || _lastValue < 0) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                ForceRefresh(_lastValue);
            };
        }
#endif
    }
}
