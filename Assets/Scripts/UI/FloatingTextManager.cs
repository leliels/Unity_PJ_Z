using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlockPuzzle.Config;
using BlockPuzzle.Core;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// 飘字管理器(M-R5,UGUI 版)。
    /// 监听 GameplayEvents,根据 FloatingTextLibrary 模板生成飘字 RectTransform。
    /// 支持 TMP 字体 / 数字图集两种渲染模式。
    ///
    /// 替代 M-R4 之前的 FloatingScoreManager(后者保留供旧 SceneBootstrap 兼容,M-R7 清理)。
    /// </summary>
    public class FloatingTextManager : MonoBehaviour
    {
        public static FloatingTextManager Instance { get; private set; }

        private FloatingTextLibrary _library;
        private RectTransform _layer;
        private readonly Dictionary<int, Queue<GameObject>> _activeByTemplate = new();

        public void Init(FloatingTextLibrary library, RectTransform layer)
        {
            _library = library;
            _layer = layer;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable() { GameplayEvents.OnEvent += HandleEvent; }
        private void OnDisable()
        {
            GameplayEvents.OnEvent -= HandleEvent;
            if (Instance == this) Instance = null;
        }

        private void HandleEvent(GameplayEventId id, GameplayEventArgs args)
        {
            if (_library == null || _layer == null) return;
            int idx = 0;
            foreach (var template in _library.GetTemplates(id))
            {
                if (template != null) StartCoroutine(SpawnAndAnimate(template, args, idx));
                idx++;
            }
        }

        private IEnumerator SpawnAndAnimate(FloatingTextLibrary.TemplateEntry template, GameplayEventArgs args, int templateIdx)
        {
            // 限流:每个模板最多 maxConcurrent
            if (!_activeByTemplate.TryGetValue(templateIdx, out var queue))
            {
                queue = new Queue<GameObject>();
                _activeByTemplate[templateIdx] = queue;
            }
            while (queue.Count >= Mathf.Max(1, template.maxConcurrent))
            {
                var oldest = queue.Dequeue();
                if (oldest != null) Destroy(oldest);
            }

            var go = BuildVisual(template, args);
            if (go == null) yield break;
            queue.Enqueue(go);

            var rt = go.GetComponent<RectTransform>();
            Vector2 startPos;
            if (args.ScreenPosition.HasValue)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _layer, args.ScreenPosition.Value, GetCanvasCamera(_layer), out startPos);
            }
            else
            {
                startPos = Vector2.zero;
            }

            Vector2 endPos = startPos + template.floatDir.normalized * template.floatDistance;
            float life = Mathf.Max(0.1f, template.lifetime);
            float t = 0f;

            // 文本组件
            TMP_Text tmp = go.GetComponent<TMP_Text>();
            Image[] digitImages = go.GetComponentsInChildren<Image>();

            while (t < life)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / life);

                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, u);
                float scale = template.scaleCurve != null ? template.scaleCurve.Evaluate(u) : 1f;
                rt.localScale = new Vector3(scale, scale, 1f);

                Color c = Color.Lerp(template.startColor, template.endColor, u);
                if (tmp != null) tmp.color = c;
                foreach (var img in digitImages) if (img != null) img.color = c;

                yield return null;
            }

            if (queue.Contains(go)) /* C# Queue 不支持 Contains 高效,简化为不剔除 */ ;
            Destroy(go);
        }

        private GameObject BuildVisual(FloatingTextLibrary.TemplateEntry template, GameplayEventArgs args)
        {
            string text = template.textFormat
                .Replace("{score}", args.IntValue.ToString())
                .Replace("{lines}", args.IntValue.ToString())
                .Replace("{multiplier}", args.IntValue2.ToString());

            var go = new GameObject("FloatingText", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_layer, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            if (template.renderMode == FloatingTextLibrary.RenderMode.TMPFont || template.digitSprites == null || template.digitSprites.Length < 10)
            {
                // TMP 渲染
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = text;
                tmp.fontSize = template.fontSize;
                tmp.color = template.startColor;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                if (template.tmpFont != null) tmp.font = template.tmpFont;
                tmp.enableAutoSizing = false;

                rt.sizeDelta = new Vector2(template.fontSize * 6, template.fontSize * 1.5f);
            }
            else
            {
                // 数字图集渲染:把 text 中的数字字符按图集摆开
                float spacing = template.fontSize * 0.55f;
                float startX = -(text.Length - 1) * spacing * 0.5f;
                int i = 0;
                foreach (var ch in text)
                {
                    var imgGo = new GameObject($"D{i}", typeof(RectTransform), typeof(Image));
                    imgGo.transform.SetParent(rt, false);
                    var imgRt = imgGo.GetComponent<RectTransform>();
                    imgRt.anchorMin = new Vector2(0.5f, 0.5f);
                    imgRt.anchorMax = new Vector2(0.5f, 0.5f);
                    imgRt.pivot = new Vector2(0.5f, 0.5f);
                    imgRt.sizeDelta = new Vector2(template.fontSize, template.fontSize);
                    imgRt.anchoredPosition = new Vector2(startX + i * spacing, 0f);

                    var img = imgGo.GetComponent<Image>();
                    img.raycastTarget = false;
                    img.color = template.startColor;
                    if (ch >= '0' && ch <= '9')
                    {
                        int digit = ch - '0';
                        if (digit < template.digitSprites.Length)
                            img.sprite = template.digitSprites[digit];
                    }
                    else
                    {
                        img.color = new Color(template.startColor.r, template.startColor.g, template.startColor.b, 0f);
                    }
                    i++;
                }
                rt.sizeDelta = new Vector2(text.Length * spacing, template.fontSize);
            }

            return go;
        }

        private static Camera GetCanvasCamera(Transform t)
        {
            var canvas = t != null ? t.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return null;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }
}
