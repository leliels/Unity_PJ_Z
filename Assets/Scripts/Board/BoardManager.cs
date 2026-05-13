using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Block;
using BlockPuzzle.Config;
using BlockPuzzle.Core;
using BlockPuzzle.Utils;

namespace BlockPuzzle.Board
{
    /// <summary>
    /// 棋盘管理器（UGUI 版,M-R2 重构）。
    ///
    /// 与旧版 SpriteRenderer 实现的区别:
    /// 1. 所有渲染都是 UI Image,在 PlayCanvas 下
    /// 2. 单元尺寸由 BoardRoot 实际宽度运行时算出,屏幕宽高比一变,棋盘自动等比缩放
    /// 3. 不再有 CellSize / CellSpacing 世界单位字段;不再有 BoardCenter 世界坐标
    /// 4. 坐标换算统一走 BoardLayout 工具
    ///
    /// 公共接口保留:Init / ClearBoard / CanPlace / PlaceBlock / IsInsideBoard / CanPlaceAny /
    /// CheckGameOver / ShowPreview / ClearPreview / ShowClearPreviewHighlight / ClearClearPreviewHighlight
    /// 事件保留:OnBlockPlaced / OnLinesCleared / OnGameOver
    /// 移除:GridToWorld / WorldToGrid(改为 BoardLayout.CellToLocal / LocalToCell)
    /// </summary>
    public class BoardManager : Singleton<BoardManager>
    {
        // ==================== Inspector 引用 ====================
        [Header("UGUI 渲染")]
        [Tooltip("棋盘根 RectTransform。挂在 PlayCanvas 下,通常带 AspectRatioFitter 锁 1:1。")]
        [SerializeField] private RectTransform _boardRoot;

        [Tooltip("单元格 Prefab(必须含 Image 组件)。运行时实例化 64 个。")]
        [SerializeField] private GameObject _uiCellPrefab;

        [Tooltip("放置预览/消除高亮 Prefab(含 Image 组件)。可选,为空时用 UICell 同一份 Prefab。")]
        [SerializeField] private GameObject _uiPreviewPrefab;

        // ==================== 事件 ====================
        public event Action<int> OnBlockPlaced;
        public event Action<int> OnLinesCleared;
        public event Action OnGameOver;

        // ==================== 棋盘数据 ====================
        private bool[,] _grid;
        private Image[,] _cellImages;
        private Color[,] _cellColors;

        // 三个层:格子层、消除高亮层、放置预览层(后者覆盖前者)
        private RectTransform _cellsContainer;
        private RectTransform _highlightContainer;
        private RectTransform _previewContainer;

        private BoardLayout _layout;
        private LayoutConfig _layoutConfig;
        private UIThemeConfig _theme;

        // ==================== 初始化 ====================

        public RectTransform BoardRoot => _boardRoot;
        public BoardLayout Layout => _layout;

        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>由 SceneBootstrap 注入配置。无配置时用默认色。</summary>
        public void Configure(LayoutConfig layoutConfig, UIThemeConfig theme)
        {
            _layoutConfig = layoutConfig;
            _theme = theme;
        }

        public void Init()
        {
            if (_boardRoot == null)
            {
                Debug.LogError("[BoardManager] _boardRoot 未配置,无法初始化。请在 BoardManager Prefab 上指定棋盘根 RectTransform。");
                return;
            }

            ClearBoardVisuals();

            int cols = _layoutConfig != null ? _layoutConfig.BoardCols : Constants.BoardCols;
            int rows = _layoutConfig != null ? _layoutConfig.BoardRows : Constants.BoardRows;
            _grid = new bool[cols, rows];
            _cellColors = new Color[cols, rows];
            _cellImages = new Image[cols, rows];

            _layout = new BoardLayout(_boardRoot, _layoutConfig);

            // 强制 layout 系统先把 BoardRoot 的 rect.width/height 算出来,
            // 否则首次 Awake 时 rect.width=0,CellSize 会算成 0,所有格子摆不出来(用户原现象)。
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_boardRoot);

            EnsureContainers();
            CreateCells();

            // 二次保险:有些情况下 ForceRebuildLayoutImmediate 不能在 Awake 同帧解析所有父级,
            // 下一帧再 RelayoutCells 一次,确保所有格子位置和大小正确。
            if (Application.isPlaying && isActiveAndEnabled)
                StartCoroutine(NextFrameRelayout());
        }

        private System.Collections.IEnumerator NextFrameRelayout()
        {
            yield return null;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_boardRoot);
            RelayoutCells();
        }

        /// <summary>
        /// 仅重新摆放已有 cell 的位置和大小,不销毁不重建。
        /// 配置(LayoutConfig)修改后调用,实现实时生效。
        /// </summary>
        public void RelayoutCells()
        {
            if (_layout == null || _cellImages == null) return;
            float size = _layout.CellSize;
            int cols = _layout.Cols;
            int rows = _layout.Rows;
            for (int col = 0; col < cols; col++)
            for (int row = 0; row < rows; row++)
            {
                var img = _cellImages[col, row];
                if (img != null) SetCellRect(img.rectTransform, col, row, size);
            }
        }

        public void ClearBoard()
        {
            Init();
        }

        private void EnsureContainers()
        {
            _cellsContainer = EnsureChildContainer("Cells", 0);
            _highlightContainer = EnsureChildContainer("ClearHighlights", 1);
            _previewContainer = EnsureChildContainer("PlacementPreview", 2);
        }

        private RectTransform EnsureChildContainer(string name, int siblingIndex)
        {
            var t = _boardRoot.Find(name) as RectTransform;
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                t = go.GetComponent<RectTransform>();
                t.SetParent(_boardRoot, false);
                t.anchorMin = Vector2.zero;
                t.anchorMax = Vector2.one;
                t.offsetMin = Vector2.zero;
                t.offsetMax = Vector2.zero;
                t.SetSiblingIndex(siblingIndex);

                // 容器不接 raycast,避免吃掉拖拽事件
                var img = go.AddComponent<Image>();
                img.color = Color.clear;
                img.raycastTarget = false;
            }
            return t;
        }

        private void ClearBoardVisuals()
        {
            if (_boardRoot == null) return;
            // 清空老的内部容器(整个重建)
            // 必须用 DestroyImmediate 同步销毁,否则下一行 EnsureContainers 的 Find 会
            // 找到正在等待销毁的旧容器,新格子摆到旧容器上,产生重叠/错位(原 bug:重开第 2 次没视觉)。
            for (int i = _boardRoot.childCount - 1; i >= 0; i--)
            {
                var child = _boardRoot.GetChild(i);
                if (child.name == "Cells" || child.name == "ClearHighlights" || child.name == "PlacementPreview")
                {
                    if (Application.isPlaying)
                    {
                        // Play 模式下 DestroyImmediate 仍然安全(它是 EditorOnly 警告但 runtime 可用,且这是我们自建对象)
                        // 用 SetParent(null) + Destroy 兜底等效同步移除
                        child.SetParent(null, false);
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
#endif
                    }
                }
            }
            _cellsContainer = null;
            _highlightContainer = null;
            _previewContainer = null;
        }

        // ==================== 创建格子 ====================

        private void CreateCells()
        {
            int cols = _layout.Cols;
            int rows = _layout.Rows;
            float size = _layout.CellSize;

            Color emptyColor = _theme != null ? _theme.CellEmptyColor : Constants.CellEmptyColor;
            // 如果策划在 UIThemeConfig 把 CellEmptyColor 配成了完全透明(alpha<0.01),
            // 用一个轻微可见的默认底色,确保玩家看得到棋盘范围(原 bug:进游戏看不到棋盘)。
            if (emptyColor.a < 0.01f)
                emptyColor = new Color(1f, 1f, 1f, 0.12f);

            // 加载棋盘格底图(brd_cell.png),让格子有美术质感而非纯色块
            var cellSprite = Utils.SpriteUtils.CellSprite;

            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    var img = CreateUIImage(_cellsContainer, $"Cell_{col}_{row}", _uiCellPrefab);
                    img.raycastTarget = false;
                    if (img.sprite == null && cellSprite != null) img.sprite = cellSprite;
                    SetCellRect(img.rectTransform, col, row, size);
                    img.color = emptyColor;

                    _cellImages[col, row] = img;
                    _cellColors[col, row] = emptyColor;
                }
            }
        }

        private void SetCellRect(RectTransform rt, int col, int row, float size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = _layout.CellToLocal(col, row);
            rt.localScale = Vector3.one;
        }

        private static Image CreateUIImage(Transform parent, string name, GameObject prefab)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, parent, false);
                go.name = name;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
            }
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            return img;
        }

        // ==================== 坐标换算辅助 ====================

        public bool IsInsideBoard(int col, int row) => _layout != null && _layout.IsInside(col, row);

        // ==================== 放置 ====================

        public bool CanPlace(Vector2Int[] cells, int originCol, int originRow)
        {
            if (cells == null || _grid == null) return false;
            foreach (var cell in cells)
            {
                int c = originCol + cell.x;
                int r = originRow + cell.y;
                if (!IsInsideBoard(c, r)) return false;
                if (_grid[c, r]) return false;
            }
            return true;
        }

        public void PlaceBlock(Vector2Int[] cells, int originCol, int originRow, Color color)
        {
            int placed = 0;
            // 累加放置位置,用作事件锚点(取重心)
            float sumX = 0f, sumY = 0f;
            var blockSprite = Utils.SpriteUtils.BlockSprite;
            foreach (var cell in cells)
            {
                int c = originCol + cell.x;
                int r = originRow + cell.y;
                _grid[c, r] = true;
                if (_cellImages[c, r] != null)
                {
                    if (blockSprite != null) _cellImages[c, r].sprite = blockSprite;
                    _cellImages[c, r].color = color;
                }
                _cellColors[c, r] = color;
                placed++;
                sumX += c; sumY += r;
            }

            // 计算放置中心的屏幕坐标(供特效/音效作锚点)
            Vector2 placeScreen = CellsCenterToScreen(originCol, originRow, cells);

            OnBlockPlaced?.Invoke(placed);
            GameplayEvents.Raise(GameplayEventId.BlockPlaced,
                new GameplayEventArgs { ScreenPosition = placeScreen, IntValue = placed });

            ClearClearPreviewHighlight();
            ClearPreview();

            var (fullRows, fullCols) = MatchChecker.CheckMatches(_grid);
            int totalLines = fullRows.Count + fullCols.Count;
            if (totalLines > 0)
            {
                Color emptyColor = _theme != null ? _theme.CellEmptyColor : Constants.CellEmptyColor;
                if (emptyColor.a < 0.01f) emptyColor = new Color(1f, 1f, 1f, 0.12f);
                var cellSprite = Utils.SpriteUtils.CellSprite;
                var clearedCells = MatchChecker.ClearLines(_grid, fullRows, fullCols);
                Vector2 clearScreen = ClearCenterToScreen(clearedCells);
                foreach (var pos in clearedCells)
                {
                    if (_cellImages[pos.x, pos.y] != null)
                    {
                        if (cellSprite != null) _cellImages[pos.x, pos.y].sprite = cellSprite;
                        _cellImages[pos.x, pos.y].color = emptyColor;
                    }
                    _cellColors[pos.x, pos.y] = emptyColor;
                }
                OnLinesCleared?.Invoke(totalLines);
                GameplayEvents.Raise(GameplayEventId.LineCleared,
                    new GameplayEventArgs { ScreenPosition = clearScreen, IntValue = totalLines });
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnTurnComplete();
        }

        // 给 cells (相对坐标) + originCol/Row 算屏幕坐标(取中心格中心)
        private Vector2 CellsCenterToScreen(int originCol, int originRow, Vector2Int[] cells)
        {
            if (cells == null || cells.Length == 0 || _layout == null) return Vector2.zero;
            float sumX = 0, sumY = 0;
            foreach (var c in cells) { sumX += c.x; sumY += c.y; }
            float avgCol = originCol + sumX / cells.Length;
            float avgRow = originRow + sumY / cells.Length;
            return CellPosToScreen(avgCol, avgRow);
        }

        private Vector2 ClearCenterToScreen(System.Collections.Generic.List<Vector2Int> cells)
        {
            if (cells == null || cells.Count == 0 || _layout == null) return Vector2.zero;
            float sumX = 0, sumY = 0;
            foreach (var c in cells) { sumX += c.x; sumY += c.y; }
            return CellPosToScreen(sumX / cells.Count, sumY / cells.Count);
        }

        private Vector2 CellPosToScreen(float col, float row)
        {
            // 用整数版 CellToLocal 内插
            int c0 = Mathf.FloorToInt(col);
            int r0 = Mathf.FloorToInt(row);
            Vector2 a = _layout.CellToLocal(c0, r0);
            Vector2 b = _layout.CellToLocal(c0 + 1, r0 + 1);
            Vector2 local = new Vector2(
                Mathf.Lerp(a.x, b.x, col - c0),
                Mathf.Lerp(a.y, b.y, row - r0));
            Vector3 world = _boardRoot.TransformPoint(local);
            var canvas = _boardRoot.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(cam, world);
        }

        // ==================== 游戏结束 ====================

        public bool CanPlaceAny(List<BlockData> candidates)
        {
            if (candidates == null || _layout == null) return false;
            int cols = _layout.Cols;
            int rows = _layout.Rows;
            foreach (var block in candidates)
            {
                if (block == null) continue;
                for (int c = 0; c < cols; c++)
                    for (int r = 0; r < rows; r++)
                        if (CanPlace(block.Cells, c, r)) return true;
            }
            return false;
        }

        public void CheckGameOver(List<BlockData> remaining)
        {
            if (!CanPlaceAny(remaining))
            {
                OnGameOver?.Invoke();
                GameplayEvents.Raise(GameplayEventId.GameOver);
            }
        }

        // ==================== 预览(放置吸附时) ====================

        private readonly List<Image> _previewImages = new List<Image>();

        public void ShowPreview(Vector2Int[] cells, int originCol, int originRow, bool valid)
        {
            ClearPreview();
            if (cells == null || _layout == null || _previewContainer == null) return;

            float size = _layout.CellSize;
            Color color = valid
                ? (_theme != null ? _theme.PreviewValidColor : Constants.PreviewValidColor)
                : (_theme != null ? _theme.PreviewInvalidColor : Constants.PreviewInvalidColor);

            // 预览也用方块贴图(blk_base.png),与放置后的视觉一致,只是颜色透明度更低
            var previewSprite = Utils.SpriteUtils.BlockSprite;

            foreach (var cell in cells)
            {
                int c = originCol + cell.x;
                int r = originRow + cell.y;
                if (!IsInsideBoard(c, r)) continue;

                var img = CreateUIImage(_previewContainer, "Preview", _uiPreviewPrefab != null ? _uiPreviewPrefab : _uiCellPrefab);
                img.raycastTarget = false;
                if (img.sprite == null && previewSprite != null) img.sprite = previewSprite;
                SetCellRect(img.rectTransform, c, r, size);
                img.color = color;
                _previewImages.Add(img);
            }
        }

        public void ClearPreview()
        {
            foreach (var img in _previewImages)
                if (img != null) Destroy(img.gameObject);
            _previewImages.Clear();
        }

        // ==================== 消除高亮 ====================

        private readonly List<Image> _highlightImages = new List<Image>();

        public void ShowClearPreviewHighlight(Vector2Int[] cells, int originCol, int originRow)
        {
            ClearClearPreviewHighlight();
            if (cells == null || _layout == null || _highlightContainer == null) return;

            int cols = _layout.Cols;
            int rows = _layout.Rows;

            var simulated = (bool[,])_grid.Clone();
            foreach (var cell in cells)
            {
                int c = originCol + cell.x;
                int r = originRow + cell.y;
                if (IsInsideBoard(c, r)) simulated[c, r] = true;
            }

            var (fullRows, fullCols) = MatchChecker.CheckMatches(simulated);
            if (fullRows.Count == 0 && fullCols.Count == 0) return;

            var set = new HashSet<(int, int)>();
            foreach (int row in fullRows)
                for (int c = 0; c < cols; c++) set.Add((c, row));
            foreach (int col in fullCols)
                for (int r = 0; r < rows; r++) set.Add((col, r));

            float size = _layout.CellSize;
            Color hl = _theme != null ? _theme.ClearPreviewHighlightColor : Constants.ClearPreviewHighlightColor;
            var hlSprite = Utils.SpriteUtils.BlockSprite;

            foreach (var (col, row) in set)
            {
                var img = CreateUIImage(_highlightContainer, "ClearHighlight", _uiPreviewPrefab != null ? _uiPreviewPrefab : _uiCellPrefab);
                img.raycastTarget = false;
                if (img.sprite == null && hlSprite != null) img.sprite = hlSprite;
                SetCellRect(img.rectTransform, col, row, size);
                img.color = hl;
                _highlightImages.Add(img);
            }
        }

        public void ClearClearPreviewHighlight()
        {
            foreach (var img in _highlightImages)
                if (img != null) Destroy(img.gameObject);
            _highlightImages.Clear();
        }

        // ==================== 屏幕坐标 → 行列 ====================

        /// <summary>
        /// 屏幕像素 → 棋盘行列。pressEventCamera 应为 PlayCanvas.worldCamera(ScreenSpaceCamera 模式),
        /// 或 null(ScreenSpaceOverlay 模式)。是否在棋盘内 = 返回值 inside && IsInsideBoard。
        /// </summary>
        public bool ScreenToCell(Vector2 screenPos, Camera pressEventCamera, out Vector2Int cell, out Vector2 localPoint)
        {
            if (_layout == null)
            {
                cell = new Vector2Int(-1, -1);
                localPoint = Vector2.zero;
                return false;
            }
            return _layout.ScreenToCell(screenPos, pressEventCamera, out cell, out localPoint);
        }
    }
}
