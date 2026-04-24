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

        /// <summary>外部设置 Cell Prefab（供 SceneBootstrap 代码注入）</summary>
        public void SetCellPrefab(GameObject prefab) { if (_cellPrefab == null) _cellPrefab = prefab; }
        /// <summary>外部设置 Preview Prefab</summary>
        public void SetPreviewPrefab(GameObject prefab) { if (_previewPrefab == null) _previewPrefab = prefab; }

        // --- 棋盘的世界坐标起点（左下角） ---
        private Vector3 _boardOrigin;

        // --- 格子总步长（大小+间距） ---
        private float CellStep => Constants.CellSize + Constants.CellSpacing;

        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// 初始化棋盘
        /// </summary>
        public void Init()
        {
            _grid = new bool[Constants.BoardCols, Constants.BoardRows];
            _cellColors = new Color[Constants.BoardCols, Constants.BoardRows];
            _cellRenderers = new SpriteRenderer[Constants.BoardCols, Constants.BoardRows];

            // 计算棋盘左下角世界坐标
            float boardWidth = Constants.BoardCols * CellStep - Constants.CellSpacing;
            float boardHeight = Constants.BoardRows * CellStep - Constants.CellSpacing;
            _boardOrigin = Constants.BoardCenter - new Vector3(boardWidth / 2f, boardHeight / 2f, 0f);

            CreateBoardVisuals();
        }

        /// <summary>
        /// 清除棋盘（重新开始用）
        /// </summary>
        public void ClearBoard()
        {
            if (_boardContainer != null)
                Destroy(_boardContainer.gameObject);

            Init();
        }

        // ==================== 可视化 ====================

        private void CreateBoardVisuals()
        {
            _boardContainer = new GameObject("BoardContainer").transform;
            _boardContainer.SetParent(transform);

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
                        cellGo.transform.position = GridToWorld(col, row);
                        cellGo.transform.localScale = Vector3.one * Constants.CellSize;
                        sr = cellGo.GetComponent<SpriteRenderer>();
                        if (sr == null) sr = cellGo.AddComponent<SpriteRenderer>();
                    }
                    else
                    {
                        // Fallback：代码创建（兼容无 Prefab 情况）
                        cellGo = new GameObject($"Cell_{col}_{row}");
                        cellGo.transform.SetParent(_boardContainer);
                        cellGo.transform.position = GridToWorld(col, row);
                        cellGo.transform.localScale = Vector3.one * Constants.CellSize;
                        sr = cellGo.AddComponent<SpriteRenderer>();
                        sr.sprite = cellSprite;
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
                col * CellStep + Constants.CellSize / 2f,
                row * CellStep + Constants.CellSize / 2f,
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

            int col = Mathf.RoundToInt((relX - Constants.CellSize / 2f) / CellStep);
            int row = Mathf.RoundToInt((relY - Constants.CellSize / 2f) / CellStep);

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

            // 检测消除
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
                    go = Instantiate(_previewPrefab);
                    go.name = "Preview";
                    go.transform.position = GridToWorld(c, r);
                    go.transform.localScale = Vector3.one * Constants.CellSize;
                    sr = go.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = go.AddComponent<SpriteRenderer>();
                }
                else
                {
                    // Fallback：代码创建
                    go = new GameObject("Preview");
                    go.transform.position = GridToWorld(c, r);
                    go.transform.localScale = Vector3.one * Constants.CellSize;
                    sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = SpriteUtils.CellSprite;
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

        // ==================== 运行时重新布局 ====================

        /// <summary>
        /// 运行时就地重新布局：根据当前 Constants 中的值重新计算所有格子位置和大小。
        /// 不销毁/重建对象，不影响游戏状态（占用、颜色等全部保留）。
        /// </summary>
        public void RelayoutBoard()
        {
            if (_cellRenderers == null) return;

            // 重新计算棋盘左下角世界坐标
            float boardWidth = Constants.BoardCols * CellStep - Constants.CellSpacing;
            float boardHeight = Constants.BoardRows * CellStep - Constants.CellSpacing;
            _boardOrigin = Constants.BoardCenter - new Vector3(boardWidth / 2f, boardHeight / 2f, 0f);

            for (int col = 0; col < Constants.BoardCols; col++)
            {
                for (int row = 0; row < Constants.BoardRows; row++)
                {
                    var sr = _cellRenderers[col, row];
                    if (sr == null) continue;

                    sr.transform.position = GridToWorld(col, row);
                    sr.transform.localScale = Vector3.one * Constants.CellSize;
                }
            }
        }
    }
}
