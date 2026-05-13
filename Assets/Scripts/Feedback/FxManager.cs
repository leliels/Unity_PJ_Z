using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Config;
using BlockPuzzle.Core;

namespace BlockPuzzle.Feedback
{
    /// <summary>
    /// 特效 / 震屏管理器(M-R5)。
    /// 监听 GameplayEvents,根据 FxLibrary 配置在指定锚点实例化特效 + 触发震屏。
    /// </summary>
    public class FxManager : MonoBehaviour
    {
        public static FxManager Instance { get; private set; }

        private FxLibrary _library;
        private GameplayTuning _tuning;
        private RectTransform _fxLayer;

        // 震屏状态
        private float _shakeTimeRemaining;
        private float _shakeDuration;
        private float _shakeAmplitude;
        private AnimationCurve _shakeCurve;
        private Vector3 _shakeBasePos;
        private bool _shaking;

        public void Init(FxLibrary library, GameplayTuning tuning, RectTransform fxLayer)
        {
            _library = library;
            _tuning = tuning;
            _fxLayer = fxLayer;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameplayEvents.OnEvent += HandleEvent;
        }

        private void OnDisable()
        {
            GameplayEvents.OnEvent -= HandleEvent;
            if (Instance == this) Instance = null;
        }

        private void HandleEvent(GameplayEventId id, GameplayEventArgs args)
        {
            if (_library == null) return;
            foreach (var entry in _library.GetEntries(id))
            {
                if (entry == null) continue;
                SpawnEffect(entry, args);
                TriggerShake(entry);
            }
        }

        // ==================== 特效 Prefab ====================

        private void SpawnEffect(FxLibrary.FxEntry entry, GameplayEventArgs args)
        {
            if (entry.effectPrefab == null) return;
            Vector2 screenPos = ResolveAnchor(entry.anchor, args) + entry.offset;

            // 在 FxLayer 的局部坐标系下显示
            if (_fxLayer == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _fxLayer, screenPos, GetCanvasCamera(_fxLayer), out var local);

            var go = Instantiate(entry.effectPrefab, _fxLayer, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = local;
            else go.transform.localPosition = new Vector3(local.x, local.y, 0f);

            float lifetime = Mathf.Clamp(entry.lifetime, 0.05f, 3f);
            Destroy(go, lifetime);
        }

        private Vector2 ResolveAnchor(FxLibrary.SpawnAnchor anchor, GameplayEventArgs args)
        {
            switch (anchor)
            {
                case FxLibrary.SpawnAnchor.EventPosition:
                    if (args.ScreenPosition.HasValue) return args.ScreenPosition.Value;
                    return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                case FxLibrary.SpawnAnchor.BoardCenter:
                    var boardRoot = SceneBootstrap.BoardRoot;
                    if (boardRoot != null)
                        return RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(boardRoot), boardRoot.position);
                    return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                case FxLibrary.SpawnAnchor.HudScore:
                    var hud = SceneBootstrap.HudSafeRoot;
                    if (hud != null)
                        return RectTransformUtility.WorldToScreenPoint(null, hud.position);
                    return new Vector2(Screen.width * 0.5f, Screen.height * 0.85f);
                case FxLibrary.SpawnAnchor.ScreenCenter:
                default:
                    return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }
        }

        private static Camera GetCanvasCamera(Transform t)
        {
            var canvas = t != null ? t.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return null;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        // ==================== 震屏 ====================

        private void TriggerShake(FxLibrary.FxEntry entry)
        {
            if (entry.shakeAmplitude <= 0f || entry.shakeDuration <= 0f) return;
            // 玩法微调:总开关
            if (_tuning != null && !_tuning.EnableHapticFeedback) return;

            // 简化实现:对 PlayCanvas 自身做位移震动。即使没有 PlayCanvas,也可降级到 Camera 震动。
            var target = (Transform)SceneBootstrap.PlayCanvas?.transform ?? Camera.main?.transform;
            if (target == null) return;

            if (!_shaking) _shakeBasePos = target.localPosition;
            _shakeTimeRemaining = entry.shakeDuration;
            _shakeDuration = entry.shakeDuration;
            _shakeAmplitude = entry.shakeAmplitude;
            _shakeCurve = entry.shakeCurve ?? AnimationCurve.EaseInOut(0, 1, 1, 0);
            _shaking = true;
            _shakeTarget = target;
        }

        private Transform _shakeTarget;

        private void LateUpdate()
        {
            if (!_shaking || _shakeTarget == null) return;

            _shakeTimeRemaining -= Time.deltaTime;
            if (_shakeTimeRemaining <= 0f)
            {
                _shakeTarget.localPosition = _shakeBasePos;
                _shaking = false;
                return;
            }

            float t = 1f - (_shakeTimeRemaining / _shakeDuration);
            float magnitude = _shakeAmplitude * _shakeCurve.Evaluate(t);
            float dx = (Random.value * 2f - 1f) * magnitude;
            float dy = (Random.value * 2f - 1f) * magnitude;
            _shakeTarget.localPosition = _shakeBasePos + new Vector3(dx, dy, 0f);
        }
    }
}
