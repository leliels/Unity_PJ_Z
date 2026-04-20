using UnityEngine;
using BlockPuzzle.Board;
using BlockPuzzle.Utils;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BlockPuzzle.Block
{
    /// <summary>
    /// 方块拖拽逻辑：兼容新版 Input System 和旧版 Input Manager。
    /// 不再依赖 OnMouseXxx（它们只在旧版 Input Manager 下有效）。
    /// </summary>
    public class BlockDrag : MonoBehaviour
    {
        private BlockData _blockData;
        private int _colorIndex;
        private int _candidateIndex;
        private Color _blockColor;

        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private bool _isDragging;
        private Camera _mainCam;
        private Collider2D _collider;

        // 拖拽时方块锚点相对鼠标的固定偏移：向上抬高一些，避免手指/光标遮挡
        // 并向左下偏移半格，让"鼠标尖端"对齐方块左下角格子的中心
        private static readonly Vector3 DragAnchorOffset = new Vector3(0f, 1.5f, 0f);

        // 当前预览位置
        private Vector2Int _lastPreviewGrid = new Vector2Int(-999, -999);

        public void Init(BlockData data, int colorIndex, int candidateIndex)
        {
            _blockData = data;
            _colorIndex = colorIndex;
            _candidateIndex = candidateIndex;
            _blockColor = Constants.BlockColors[colorIndex];
        }

        private bool _initialized;

        private void Start()
        {
            EnsureInit();
        }

        private void EnsureInit()
        {
            if (_initialized) return;
            _mainCam = Camera.main;
            if (_mainCam == null) return;
            _collider = GetComponent<Collider2D>();
            if (_collider == null) return;
            _originalPosition = transform.position;
            _originalScale = transform.localScale;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized)
            {
                EnsureInit();
                if (!_initialized) return;
            }
            // 游戏结束时禁用交互
            if (Core.GameManager.Instance != null &&
                Core.GameManager.Instance.CurrentState != Core.GameState.Playing)
            {
                if (_isDragging) CancelDrag();
                return;
            }

            bool pointerDown = GetPointerDownThisFrame();
            bool pointerHeld = GetPointerHeld();
            bool pointerUp = GetPointerUpThisFrame();

            if (!_isDragging && pointerDown)
            {
                // 射线检测：点击的是否是自己
                Vector3 pointerWorld = GetPointerWorldPos();
                var hit = Physics2D.OverlapPoint(pointerWorld);
                if (hit != null && hit == _collider)
                {
                    BeginDrag(pointerWorld);
                }
            }
            else if (_isDragging && pointerHeld)
            {
                DragUpdate();
            }
            else if (_isDragging && pointerUp)
            {
                EndDrag();
            }
        }

        // ==================== 拖拽流程 ====================

        private void BeginDrag(Vector3 pointerWorld)
        {
            _isDragging = true;
            // 拖拽时放大到原始大小（候选区是缩小显示）
            transform.localScale = Vector3.one;
            // 立即把方块锚点对齐到"鼠标指向的格子中心"上方
            SnapToPointer(pointerWorld);
        }

        private void DragUpdate()
        {
            Vector3 pointerWorld = GetPointerWorldPos();
            SnapToPointer(pointerWorld);
            UpdatePreview();
        }

        /// <summary>
        /// 将方块锚点对齐到鼠标位置（加固定偏移）。
        /// 方块锚点 = cell(0,0) 的世界位置 = 左下角格子中心，
        /// 这样鼠标位置直接决定方块放置的 origin 格。
        /// </summary>
        private void SnapToPointer(Vector3 pointerWorld)
        {
            transform.position = pointerWorld + DragAnchorOffset;
        }

        private void EndDrag()
        {
            _isDragging = false;

            if (BoardManager.Instance != null)
                BoardManager.Instance.ClearPreview();

            // 尝试放置
            Vector3 blockWorldPos = transform.position;
            Vector2Int gridPos = BoardManager.Instance.WorldToGrid(blockWorldPos);

            if (BoardManager.Instance.CanPlace(_blockData.Cells, gridPos.x, gridPos.y))
            {
                BoardManager.Instance.PlaceBlock(_blockData.Cells, gridPos.x, gridPos.y, _blockColor);
                BlockSpawner.Instance.MarkUsed(_candidateIndex);

                var remaining = BlockSpawner.Instance.GetRemainingCandidates();
                BoardManager.Instance.CheckGameOver(remaining);

                Destroy(gameObject);
            }
            else
            {
                // 放置失败，回到原位
                transform.position = _originalPosition;
                transform.localScale = _originalScale;
            }
        }

        private void CancelDrag()
        {
            _isDragging = false;
            if (BoardManager.Instance != null)
                BoardManager.Instance.ClearPreview();
            transform.position = _originalPosition;
            transform.localScale = _originalScale;
        }

        // ==================== 预览 ====================

        private void UpdatePreview()
        {
            Vector3 blockWorldPos = transform.position;
            Vector2Int gridPos = BoardManager.Instance.WorldToGrid(blockWorldPos);

            if (gridPos == _lastPreviewGrid) return;
            _lastPreviewGrid = gridPos;

            bool valid = BoardManager.Instance.CanPlace(_blockData.Cells, gridPos.x, gridPos.y);
            BoardManager.Instance.ShowPreview(_blockData.Cells, gridPos.x, gridPos.y, valid);
        }

        // ==================== 输入抽象（兼容新旧输入系统） ====================

        private Vector3 GetPointerWorldPos()
        {
            Vector2 screenPos = GetPointerScreenPos();
            Vector3 v = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(_mainCam.transform.position.z));
            return _mainCam.ScreenToWorldPoint(v);
        }

        private Vector2 GetPointerScreenPos()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            return Vector2.zero;
#else
            return (Vector2)Input.mousePosition;
#endif
        }

        private bool GetPointerDownThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            return false;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private bool GetPointerHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;
            return false;
#else
            return Input.GetMouseButton(0);
#endif
        }

        private bool GetPointerUpThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                return true;
            return false;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }
    }
}
