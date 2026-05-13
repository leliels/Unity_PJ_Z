using UnityEngine;

namespace BlockPuzzle.UI.Layout
{
    /// <summary>
    /// 把一个 RectTransform 的 anchor 收缩到 Screen.safeArea 范围内,
    /// 适配 iPhone 刘海 / Dynamic Island / 底部 HomeIndicator。
    ///
    /// 用法:挂在某个 Canvas 的根子节点上(通常叫 SafeAreaRoot),
    /// 然后把 HUD/Overlay 内容放进去即可。Canvas 本身不变。
    ///
    /// 注意:挂载对象必须是 Canvas 的直接子节点(或同级),
    /// 因为 anchor 计算是相对父级的 RectTransform。
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        private void OnEnable()
        {
            _rect = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            // 屏幕旋转 / 分辨率切换时重新应用
            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastScreen.x ||
                Screen.height != _lastScreen.y)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            if (_rect == null) return;

            _lastSafeArea = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);

            Vector2 anchorMin = _lastSafeArea.position;
            Vector2 anchorMax = anchorMin + _lastSafeArea.size;

            float w = _lastScreen.x;
            float h = _lastScreen.y;
            if (w <= 0f || h <= 0f) return;

            anchorMin.x /= w;
            anchorMin.y /= h;
            anchorMax.x /= w;
            anchorMax.y /= h;

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
