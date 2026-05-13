using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BlockPuzzle.Audio;
using BlockPuzzle.Board;
using BlockPuzzle.Config;
using BlockPuzzle.Core;
using BlockPuzzle.Utils;

namespace BlockPuzzle.Block
{
    /// <summary>
    /// 方块拖拽(UGUI 版,M-R3 重构,M-R7+ 修复)。
    ///
    /// 修复点:
    /// 1. 拖拽偏移(X/Y)挪到 GameplayTuning,策划/美术可调
    /// 2. 预览位置用"方块中心点"而不是"手指原始位置"——这样预览紧贴方块,不会跑到手指下方
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BlockDrag : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BlockData _blockData;
        private int _colorIndex;
        private Color _blockColor;
        private int _candidateIndex;
        private BlockSpawner _spawner;

        private RectTransform _rt;
        private RectTransform _originalParent;
        private Vector2 _originalAnchoredPos;
        private Vector3 _originalScale;
        private Vector2 _originalSizeDelta;
        private Vector2 _originalAnchorMin, _originalAnchorMax, _originalPivot;

        private CanvasGroup _canvasGroup;
        private Canvas _rootCanvas;
        private RectTransform _dragLayer;

        private BlockAudioFeedback _audioFeedback;
        private Vector2Int _lastPreviewCell = new Vector2Int(-9999, -9999);
        private bool _isDragging;

        public void Init(BlockData data, int colorIndex, Color color, int candidateIndex, BlockSpawner spawner)
        {
            _blockData = data;
            _colorIndex = colorIndex;
            _blockColor = color;
            _candidateIndex = candidateIndex;
            _spawner = spawner;
        }

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _audioFeedback = GetComponent<BlockAudioFeedback>();
        }

        // ==================== Pointer 事件 ====================

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsGamePlaying()) return;
            _audioFeedback?.PlayPick();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsGamePlaying()) return;
            _isDragging = true;
            _audioFeedback?.PlayDragBegin();

            CacheOriginalTransform();
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            _dragLayer = ResolveDragLayer();

            // Reparent 到 DragLayer。先把 anchor/pivot 切成中心固定模式,
            // 否则原 Slot 的"撑满父级"anchor 在 DragLayer 下会让方块铺满整个屏幕。
            float currentWidth = _rt.rect.width;
            float currentHeight = _rt.rect.height;

            transform.SetParent(_dragLayer != null ? _dragLayer : (Transform)_rootCanvas?.transform, true);

            _rt.anchorMin = new Vector2(0.5f, 0.5f);
            _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.sizeDelta = new Vector2(currentWidth, currentHeight);

            // 拖拽期间 raycast 透传,不阻挡棋盘事件检测
            _canvasGroup.blocksRaycasts = false;

            // 拖拽时方块缩放到棋盘格的实际大小
            ApplyBoardScale();
            UpdateDragPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            UpdateDragPosition(eventData);
            UpdatePreview(eventData.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            BoardManager.Instance?.ClearPreview();
            BoardManager.Instance?.ClearClearPreviewHighlight();
            _canvasGroup.blocksRaycasts = true;

            if (TryPlaceAtPointer(eventData))
            {
                _audioFeedback?.PlayDropSuccess();
                BoardManager.Instance.PlaceBlock(_blockData.Cells, _placeOriginCol, _placeOriginRow, _blockColor);
                _spawner.MarkUsed(_candidateIndex);

                var remaining = _spawner.GetRemainingCandidates();
                BoardManager.Instance.CheckGameOver(remaining);

                Destroy(gameObject);
            }
            else
            {
                _audioFeedback?.PlayDropFailed();
                ReturnToOrigin();
            }
        }

        // ==================== 内部:位置 / 预览 ====================

        private int _placeOriginCol, _placeOriginRow;
        private bool _placeValid;

        private void UpdateDragPosition(PointerEventData eventData)
        {
            if (_rootCanvas == null) return;

            // 把屏幕坐标转成 DragLayer(或 rootCanvas)的局部坐标
            var parent = (RectTransform)transform.parent;
            if (parent == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, eventData.position, eventData.pressEventCamera, out var local);

            // 由 GameplayTuning 决定拖拽偏移(策划/美术可在 .asset 里调)
            float boardCellSize = BoardManager.Instance?.Layout?.CellSize ?? 80f;
            var tuning = SceneBootstrap.ActiveConfig?.Gameplay;
            float offX = tuning != null ? tuning.DragOffsetXInCells : 0f;
            float offY = tuning != null ? tuning.DragOffsetYInCells : 1.5f;
            local.x += boardCellSize * offX;
            local.y += boardCellSize * offY;
            _rt.anchoredPosition = local;
        }

        private void UpdatePreview(Camera pressCamera)
        {
            var board = BoardManager.Instance;
            if (board == null) return;

            // 把方块"几何中心"对应的屏幕坐标转换成棋盘行列
            // 方块的 RectTransform 中心 = transform.position(屏幕空间)
            Vector3 worldCenter = _rt.position;
            // 把方块中心点位置投到屏幕,再用 Board 提供的 ScreenToCell 反算
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), worldCenter);

            if (!board.ScreenToCell(screenPos, GetCanvasCamera(), out var centerCell, out var localPoint))
            {
                _placeValid = false;
                board.ClearPreview();
                board.ClearClearPreviewHighlight();
                _lastPreviewCell = new Vector2Int(-9999, -9999);
                return;
            }

            // 方块中心 → 形状原点(左下角格子)
            var (minX, minY, maxX, maxY) = ShapeBounds(_blockData);
            int widthCells = maxX - minX + 1;
            int heightCells = maxY - minY + 1;
            int originCol = centerCell.x - (widthCells - 1) / 2;
            int originRow = centerCell.y - (heightCells - 1) / 2;

            if (originCol == _lastPreviewCell.x && originRow == _lastPreviewCell.y) return;
            _lastPreviewCell = new Vector2Int(originCol, originRow);

            _placeOriginCol = originCol;
            _placeOriginRow = originRow;
            _placeValid = board.CanPlace(_blockData.Cells, originCol, originRow);

            board.ShowPreview(_blockData.Cells, originCol, originRow, _placeValid);
            if (_placeValid)
                board.ShowClearPreviewHighlight(_blockData.Cells, originCol, originRow);
            else
                board.ClearClearPreviewHighlight();
        }

        private bool TryPlaceAtPointer(PointerEventData eventData)
        {
            UpdatePreview(eventData.pressEventCamera);
            return _placeValid;
        }

        private void ReturnToOrigin()
        {
            transform.SetParent(_originalParent, false);
            _rt.anchorMin = _originalAnchorMin;
            _rt.anchorMax = _originalAnchorMax;
            _rt.pivot = _originalPivot;
            _rt.sizeDelta = _originalSizeDelta;
            _rt.anchoredPosition = _originalAnchoredPos;
            _rt.localScale = _originalScale;
        }

        // ==================== 辅助 ====================

        private void CacheOriginalTransform()
        {
            _originalParent = (RectTransform)transform.parent;
            _originalAnchoredPos = _rt.anchoredPosition;
            _originalScale = _rt.localScale;
            _originalSizeDelta = _rt.sizeDelta;
            _originalAnchorMin = _rt.anchorMin;
            _originalAnchorMax = _rt.anchorMax;
            _originalPivot = _rt.pivot;
        }

        private void ApplyBoardScale()
        {
            // 拖拽时把方块每格缩放到与棋盘格一致
            var board = BoardManager.Instance;
            if (board == null || board.Layout == null) return;
            float boardCellSize = board.Layout.CellSize;
            // 当前方块每格大小 = "Cells" 容器子节点的 sizeDelta.x
            var cellsRt = _rt.Find("Cells") as RectTransform;
            if (cellsRt == null || cellsRt.childCount == 0) return;
            var sample = cellsRt.GetChild(0) as RectTransform;
            if (sample == null || sample.sizeDelta.x <= 0f) return;

            float currentCell = sample.sizeDelta.x * _rt.localScale.x;
            if (currentCell <= 0f) return;

            float scaleFactor = boardCellSize / currentCell;
            _rt.localScale = _originalScale * scaleFactor;
        }

        private RectTransform ResolveDragLayer()
        {
            if (_rootCanvas == null) return null;
            // 约定:Canvas 下名为 "DragLayer" 的子节点用作拖拽层。不存在则现场创建。
            var existing = _rootCanvas.transform.Find("DragLayer") as RectTransform;
            if (existing != null) return existing;

            var go = new GameObject("DragLayer", typeof(RectTransform));
            existing = go.GetComponent<RectTransform>();
            existing.SetParent(_rootCanvas.transform, false);
            existing.anchorMin = Vector2.zero;
            existing.anchorMax = Vector2.one;
            existing.offsetMin = Vector2.zero;
            existing.offsetMax = Vector2.zero;
            existing.SetAsLastSibling();
            return existing;
        }

        private Camera GetCanvasCamera()
        {
            if (_rootCanvas == null) return null;
            return _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
        }

        private static (int minX, int minY, int maxX, int maxY) ShapeBounds(BlockData data)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var c in data.Cells)
            {
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
            }
            return (minX, minY, maxX, maxY);
        }

        private static bool IsGamePlaying()
        {
            return GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing;
        }
    }
}
