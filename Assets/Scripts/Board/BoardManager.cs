using System;
using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Core;
using BlockPuzzle.Block;
using BlockPuzzle.Utils;

namespace BlockPuzzle.Board
{
    /// <summary>
    /// 棋盘管理器：创建网格、处理放置、调用消除、检查游戏结束
    /// </summary>
    public class BoardManager : Singleton<BoardManager>
    {
        // ==================== Inspector 可调参数 ====================
        [Header("棋盘布局参数")]
        [Tooltip("每个格子的世界单位大小")]
        [SerializeField] private float _cellSize = 1f;
        [Tooltip("格子之间的间距")]
        [SerializeField] private float _cellSpacing = 0.08f;
        [Tooltip("棋盘中心世界坐标")]
        [SerializeField] private Vector3 _boardCenter;
        [Tooltip("棋盘整体视觉缩放（1.0=原始大小）")]
        [SerializeField] private float _visualScale = 1.12f;

        // --- 运行时有效值 ---
        /// <summary>运行时格子大小</summary>
        public float CellSize => _cellSize;
        /// <summary>运行时间距</summary>
        public float CellSpacing => _cellSpacing;
        /// <summary>运行时棋盘中心</summary>
        public Vector3 BoardCenter => _boardCenter;
        /// <summary>运行时视觉缩放</summary>
        public float VisualScale => _visualScale;

        // --- Prefab 引用（Inspector 可配置） ---
        [Header("Prefab 配置")]
        [Tooltip("棋盘格子 Prefab（需含 SpriteRenderer）。为空时使用代码创建 fallback。")]
        [SerializeField] private GameObject _cellPrefab;

        [Tooltip("预览格子 Prefab（需含 SpriteRenderer）。为空时使用代码创建 fallback。")]
        [SerializeField] private GameObject _previewPrefab;

        // --- 事件 ---
        /// <summary>放置方块后触发（参数：放置的格子数）</summary>
        public event Action<int> OnBlockPlaced;
        /// <summary>消除行/列后触发（参数：消除的行/列总数）</summary>
        public event Action<int> OnLinesCleared;
        /// <summary>游戏结束触发</summary>
        public event Action OnGameOver;

        // --- 棋盘数据 ---
        private bool[,] _grid;                          // [col, row] 占用状态
        private SpriteRenderer[,] _cellRenderers;       // [col, row] 格子渲染器
        private Color[,] _cellColors;                   // [col, row] 格子颜色记录
        private Transform _boardContainer;
        private Transform _boardScaleRoot;              // 整体缩放根节点

        /// <summary>外部设置 Cell Prefab（供 SceneBootstrap 代码注入）</summary>
        public void SetCellPrefab(GameObject prefab) { if (_cellPrefab == null) _cellPrefab = prefab; }
        /// <summary>外部设置 Preview Prefab</summary>
        public void SetPreviewPrefab(GameObject prefab) { if (_previewPrefab == null) _previewPrefab = prefab; }

        // --- 棋盘的世界坐标起点（左下角） ---
        private Vector3 _boardOrigin;

        // --- 格子总步长（大小+间距） ---
        private float CellStep => CellSize + CellSpacing;

        protected override void Awake()
        {
            base.Awake();
        }

#if UNITY_EDITOR
        /// <summary>Inspector 参数变化时自动重新布局（编辑器模式 + Play 模式均生效）</summary>
        private void OnValidate()
        {
            if (_boardContainer == null) return;
            RelayoutBoard();
        }
#endif

        /// <summary>
        /// 手动刷新棋盘布局（运行时调用）
        /// </summary>
        public void RefreshLayout() => RelayoutBoard();

        /// <summary>
        /// 初始化棋盘
        /// </summary>
        public void Init()
        {
            _grid = new bool[Constants.BoardCols, Constants.BoardRows];
            _cellColors = new Color[Constants.BoardCols, Constants.BoardRows];
            _cellRenderers = new SpriteRenderer[Constants.BoardCols, Constants.BoardRows];

            // 计算棋盘左下角世界坐标
            float boardWidth = Constants.BoardCols * CellStep - CellSpacing;
            float boardHeight = Constants.BoardRows * CellStep - CellSpacing;
            _boardOrigin = BoardCenter - new Vector3(boardWidth / 2f, boardHeight / 2f, 0f);

            CreateBoardVisuals();
        }

        /// <summary>
        /// 清除棋盘（重新开始用）
        /// </summary>
        public void ClearBoard()
        {
            if (_boardScaleRoot != null)
                Destroy(_boardScaleRoot.gameObject);

            Init();
        }

        // ==================== 可视化 ====================

        private void CreateBoardVisuals()
        {
            // 1. 缩放根节点：锚点在棋盘中心，整体缩放
            _boardScaleRoot = new GameObject("BoardScaleRoot").transform;
            _boardScaleRoot.SetParent(transform);
            _boardScaleRoot.position = BoardCenter;
            _boardScaleRoot.localScale = Vector3.one * VisualScale;

            // 2. 棋盘容器（格子父节点），局部坐标偏移使格子围绕中心分布
            float boardWidth = Constants.BoardCols * CellStep - CellSpacing;
            float boardHeight = Constants.BoardRows * CellStep - CellSpacing;
            _boardContainer = new GameObject("BoardContainer").transform;
            _boardContainer.SetParent(_boardScaleRoot);
            _boardContainer.localPosition = new Vector3(-boardWidth / 2f, -boardHeight / 2f, 0f);
            _boardContainer.localScale = Vector3.one;

            Sprite cellSprite = SpriteUtils.CellSprite;

            for (int col = 0; col < Constants.BoardCols; col++)
            {
                for (int row = 0; row < Constants.BoardRows; row++)
                {
                    GameObject cellGo;
                    SpriteRenderer sr;

                    if (_cellPrefab != null)
                    {
                        // Prefab 方式：实例化预制体
                        cellGo = Instantiate(_cellPrefab, _boardContainer);
                        cellGo.name = $"Cell_{col}_{row}";
                        // 局部坐标（相对于 BoardContainer），原点在左下角
                        cellGo.transform.localPosition = new Vector3(
                            col * CellStep + CellSize / 2f,
                            row * CellStep + CellSize / 2f,
                            0f
                        );
                        cellGo.transform.localScale = Vector3.one;
                        sr = cellGo.GetComponent<SpriteRenderer>();
                        if (sr == null) sr = cellGo.AddComponent<SpriteRenderer>();
                        sr.size = new Vector2(CellSize, CellSize);
                    }
                    else
                    {
                        // Fallback：代码创建（兼容无 Prefab 情况）
                        cellGo = new GameObject($"Cell_{col}_{row}");
                        cellGo.transform.SetParent(_boardContainer);
                        cellGo.transform.localPosition = new Vector3(
                            col * CellStep + CellSize / 2f,
                            row * CellStep + CellSize / 2f,
                            0f
                        );
                        cellGo.transform.localScale = Vector3.one;
                        sr = cellGo.AddComponent<SpriteRenderer>();
                        sr.sprite = cellSprite;
                        sr.size = new Vector2(CellSize, CellSize);
                    }

                    sr.color = Constants.CellEmptyColor;
                    sr.sortingOrder = 0;

                    _cellRenderers[col, row] = sr;
                    _cellColors[col, row] = Constants.CellEmptyColor;
                }
            }
        }

        // ==================== 坐标转换 ====================

        /// <summary>
        /// 网格坐标转世界坐标
        /// </summary>
        public Vector3 GridToWorld(int col, int row)
        {
            return _boardOrigin + new Vector3(
                col * CellStep + CellSize / 2f,
                row * CellStep + CellSize / 2f,
                0f
            );
        }

        /// <summary>
        /// 世界坐标转网格坐标（最近的格子）
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float relX = worldPos.x - _boardOrigin.x;
            float relY = worldPos.y - _boardOrigin.y;

            int col = Mathf.RoundToInt((relX - CellSize / 2f) / CellStep);
            int row = Mathf.RoundToInt((relY - CellSize / 2f) / CellStep);

            return new Vector2Int(col, row);
        }

        /// <summary>
        /// 检查网格坐标是否在棋盘范围内
        /// </summary>
        public bool IsInsideBoard(int col, int row)
        {
            return col >= 0 && col < Constants.BoardCols && row >= 0 && row < Constants.BoardRows;
        }

        // ==================== 放置逻辑 ====================

        /// <summary>
        /// 判断方块是否可以放置到指定位置
        /// </summary>
        /// <param name="cells">方块形状的相对坐标</param>
        /// <param name="originCol">放置原点列</param>
        /// <param name="originRow">放置原点行</param>
        public bool CanPlace(Vector2Int[] cells, int originCol, int originRow)
        {
            foreach (var cell in cells)
            {
                int c = originCol + cell.x;
                int r = originRow + cell.y;

                if (!IsInsideBoard(c, r)) return false;
                if (_grid[c, r]) return false;
            }
            return true;
        }

        /// <summary>
        /// 放置方块到棋盘
        /// </summary>
        public void PlaceBlock(Vector2Int[] cells, int originCol, int originRow, Color color)
        {
            int placedCount = 0;

            foreach (var cell in cells)
            {
                int c = originCol + cell.x;
                int r = originRow + cell.y;

                _grid[c, r] = true;
                _cellRenderers[c, r].color = color;
                _cellColors[c, r] = color;
                placedCount++;
            }

            OnBlockPlaced?.Invoke(placedCount);

            // 先清除预览阶段的高亮（防止残留）
            ClearClearPreviewHighlight();

            // 检测并立即执行消除
            var (fullRows, fullCols) = MatchChecker.CheckMatches(_grid);
            int totalLines = fullRows.Count + fullCols.Count;

            if (totalLines > 0)
            {
                var clearedCells = MatchChecker.ClearLines(_grid, fullRows, fullCols);

                // 更新视觉
                foreach (var pos in clearedCells)
                {
                    _cellRenderers[pos.x, pos.y].color = Constants.CellEmptyColor;
                    _cellColors[pos.x, pos.y] = Constants.CellEmptyColor;
                }

                OnLinesCleared?.Invoke(totalLines);
            }

            // 通知 GameManager 本回合结束（用于 Combo 中断判定）
            if (Core.GameManager.Instance != null)
                Core.GameManager.Instance.OnTurnComplete();
        }

        // ==================== 游戏结束判定 ====================

        /// <summary>
        /// 检查指定的候选方块列表中是否有任何一个能放到棋盘上
        /// </summary>
        public bool CanPlaceAny(List<BlockData> candidates)
        {
            foreach (var block in candidates)
            {
                if (block == null) continue;
                for (int col = 0; col < Constants.BoardCols; col++)
                {
                    for (int row = 0; row < Constants.BoardRows; row++)
                    {
                        if (CanPlace(block.Cells, col, row))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 放置后检查游戏是否结束
        /// </summary>
        public void CheckGameOver(List<BlockData> remainingCandidates)
        {
            if (!CanPlaceAny(remainingCandidates))
            {
                OnGameOver?.Invoke();
            }
        }

        // ==================== 预览 ====================

        private List<SpriteRenderer> _previewRenderers = new List<SpriteRenderer>();

        /// <summary>
        /// 显示放置预览
        /// </summary>
        public void ShowPreview(Vector2Int[] cells, int originCol, int originRow, bool valid)
        {
            ClearPreview();

            Color previewColor = valid ? Constants.PreviewValidColor : Constants.PreviewInvalidColor;

            foreach (var cell in cells)
            {
                int c = originCol + cell.x;
                int r = originRow + cell.y;

                if (!IsInsideBoard(c, r)) continue;

                GameObject go;
                SpriteRenderer sr;

                if (_previewPrefab != null)
                    {
                        // Prefab 方式
                        go = Instantiate(_previewPrefab, _boardScaleRoot);
                        go.name = "Preview";
                        go.transform.localPosition = new Vector3(
                            c * CellStep + CellSize / 2f,
                            r * CellStep + CellSize / 2f,
                            0f
                        ) + _boardContainer.localPosition;
                        go.transform.localScale = Vector3.one;
                        sr = go.GetComponent<SpriteRenderer>();
                        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
                        sr.size = new Vector2(CellSize, CellSize);
                    }
                    else
                    {
                        // Fallback：代码创建
                        go = new GameObject("Preview");
                        go.transform.SetParent(_boardScaleRoot);
                        go.transform.localPosition = new Vector3(
                            c * CellStep + CellSize / 2f,
                            r * CellStep + CellSize / 2f,
                            0f
                        ) + _boardContainer.localPosition;
                        go.transform.localScale = Vector3.one;
                        sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = SpriteUtils.CellSprite;
                        sr.size = new Vector2(CellSize, CellSize);
                    }

                sr.color = previewColor;
                sr.sortingOrder = 5;

                _previewRenderers.Add(sr);
            }
        }

        /// <summary>
        /// 清除预览
        /// </summary>
        public void ClearPreview()
        {
            foreach (var sr in _previewRenderers)
            {
                if (sr != null) Destroy(sr.gameObject);
            }
            _previewRenderers.Clear();
        }

        // ==================== 消除预览高亮（拖拽时提示） ====================

        private List<SpriteRenderer> _clearHighlightRenderers = new List<SpriteRenderer>();

        /// <summary>
        /// 显示消除预览高亮：模拟放置方块后，检测哪些行/列会被填满，并用高亮颜色标记。
        /// 在拖拽预览阶段调用，给玩家"放这里可以消除"的视觉反馈。
        /// </summary>
        public void ShowClearPreviewHighlight(Vector2Int[] cells, int originCol, int originRow)
        {
            ClearClearPreviewHighlight();

            // 模拟放置，临时标记格子
            var simulatedGrid = (bool[,])_grid.Clone();
            foreach (var cell in cells)
            {
                int c = originCol + cell.x;
                int r = originRow + cell.y;
                if (IsInsideBoard(c, r))
                    simulatedGrid[c, r] = true;
            }

            // 检测模拟放置后的满行/满列
            var (fullRows, fullCols) = MatchChecker.CheckMatches(simulatedGrid);
            if (fullRows.Count == 0 && fullCols.Count == 0) return;

            // 收集需要高亮的格子坐标（去重）
            var highlightSet = new HashSet<(int, int)>();
            foreach (int row in fullRows)
                for (int col = 0; col < Constants.BoardCols; col++)
                    highlightSet.Add((col, row));
            foreach (int col in fullCols)
                for (int row = 0; row < Constants.BoardRows; row++)
                    highlightSet.Add((col, row));

            // 为每个要消除的格子创建高亮覆盖层（不修改原始格子颜色）
            Color hlColor = Constants.ClearPreviewHighlightColor;
            foreach (var (col, row) in highlightSet)
            {
                GameObject go = new GameObject("ClearHighlight");
                go.transform.SetParent(_boardScaleRoot);
                go.transform.localPosition = new Vector3(
                    col * CellStep + CellSize / 2f,
                    row * CellStep + CellSize / 2f,
                    0f
                ) + _boardContainer.localPosition;
                go.transform.localScale = Vector3.one;

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = SpriteUtils.CellSprite;
                    sr.size = new Vector2(CellSize, CellSize);
                    sr.color = hlColor;
                sr.sortingOrder = 3; // 在普通格子(0)之上、放置预览(5)之下

                _clearHighlightRenderers.Add(sr);
            }
        }

        /// <summary>
        /// 清除所有消除预览高亮
        /// </summary>
        public void ClearClearPreviewHighlight()
        {
            foreach (var sr in _clearHighlightRenderers)
            {
                if (sr != null) Destroy(sr.gameObject);
            }
            _clearHighlightRenderers.Clear();
        }

        // ==================== 运行时重新布局 ====================

        /// <summary>
        /// 运行时就地重新布局：根据 Inspector 参数重新计算所有格子位置和大小。
        /// 不销毁/重建对象，不影响游戏状态（占用、颜色等全部保留）。
        /// </summary>
        public void RelayoutBoard()
        {
            if (_cellRenderers == null) return;

            // 重新计算棋盘尺寸
            float boardWidth = Constants.BoardCols * CellStep - CellSpacing;
            float boardHeight = Constants.BoardRows * CellStep - CellSpacing;
            _boardOrigin = BoardCenter - new Vector3(boardWidth / 2f, boardHeight / 2f, 0f);

            // 更新缩放根节点
            if (_boardScaleRoot != null)
            {
                _boardScaleRoot.position = BoardCenter;
                _boardScaleRoot.localScale = Vector3.one * VisualScale;
            }

            // 更新棋盘容器偏移
            if (_boardContainer != null)
                _boardContainer.localPosition = new Vector3(-boardWidth / 2f, -boardHeight / 2f, 0f);

            for (int col = 0; col < Constants.BoardCols; col++)
            {
                for (int row = 0; row < Constants.BoardRows; row++)
                {
                    var sr = _cellRenderers[col, row];
                    if (sr == null) continue;

                    sr.transform.localPosition = new Vector3(
                        col * CellStep + CellSize / 2f,
                        row * CellStep + CellSize / 2f,
                        0f
                    );
                    sr.size = new Vector2(CellSize, CellSize);
                }
            }
        }
    }
}
