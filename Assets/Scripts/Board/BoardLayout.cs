using UnityEngine;
using BlockPuzzle.Config;

namespace BlockPuzzle.Board
{
    /// <summary>
    /// 棋盘布局换算的中心工具。
    /// 全工程"屏幕坐标 ↔ 棋盘行列 ↔ 棋盘 RectTransform 局部坐标"的换算唯一源头。
    ///
    /// 设计原则:
    /// - 棋盘根容器 BoardRoot 是一个 RectTransform,挂 AspectRatioFitter 锁 1:1
    /// - 单元尺寸 = BoardRoot 的实际宽度 / 列数,运行时动态计算
    /// - 不依赖世界坐标、PixelsPerUnit、相机 orthographicSize
    ///
    /// 这样无论 iPhone / iPad / 任何宽高比,棋盘格永远等比缩放,iOS 上吸附位置必定与背景重合。
    /// </summary>
    public sealed class BoardLayout
    {
        private readonly RectTransform _boardRoot;
        private readonly LayoutConfig _config;

        public BoardLayout(RectTransform boardRoot, LayoutConfig config)
        {
            _boardRoot = boardRoot;
            _config = config;
        }

        public RectTransform BoardRoot => _boardRoot;
        public int Rows => _config != null ? _config.BoardRows : Utils.Constants.BoardRows;
        public int Cols => _config != null ? _config.BoardCols : Utils.Constants.BoardCols;

        /// <summary>
        /// 棋盘正方形边长(取 BoardRoot 实时宽高的较小者,避免父级尺寸还没解析完时返回错误的扁宽矩形)。
        /// </summary>
        public float SquareSide
        {
            get
            {
                float w = _boardRoot.rect.width;
                float h = _boardRoot.rect.height;
                return Mathf.Min(Mathf.Abs(w), Mathf.Abs(h));
            }
        }

        /// <summary>每格的边长(基于 BoardRoot 当前正方形边长的实时尺寸,UI 单位)。考虑间距后的纯格子大小。</summary>
        public float CellSize
        {
            get
            {
                float spacing = _config != null ? _config.CellSpacingRatio : 0f;
                // step = cellSize + spacing*cellSize = cellSize*(1+spacing)
                // totalSteps = Cols * step - spacing*cellSize  (最后一格没有右间距,但简化为全等分)
                // 简化:squareSide = Cols * step => step = squareSide / Cols => cellSize = step / (1+spacing)
                float step = SquareSide / Cols;
                return step / (1f + spacing);
            }
        }

        /// <summary>格与格之间的视觉间距(UI 单位)。</summary>
        public float CellSpacing
        {
            get
            {
                float spacing = _config != null ? _config.CellSpacingRatio : 0f;
                return CellSize * spacing;
            }
        }

        /// <summary>相邻格子中心之间的距离(= CellSize + CellSpacing)。</summary>
        public float CellStep => SquareSide / Cols;

        /// <summary>
        /// 行列 → BoardRoot 局部 anchoredPosition(以 BoardRoot 中心为 (0,0))。
        /// 即使 BoardRoot 不是严格正方形,我们也按"内切正方形"居中摆放格子。
        /// </summary>
        public Vector2 CellToLocal(int col, int row)
        {
            float step = CellStep;
            float halfBoard = step * Cols * 0.5f;
            float originX = -halfBoard + step * 0.5f;
            float originY = -halfBoard + step * 0.5f;
            return new Vector2(originX + col * step, originY + row * step);
        }

        /// <summary>
        /// BoardRoot 局部坐标 → 行列(最近格子取整)。
        /// </summary>
        public Vector2Int LocalToCell(Vector2 localPoint)
        {
            float step = CellStep;
            float halfBoard = step * Cols * 0.5f;
            float relX = localPoint.x + halfBoard;
            float relY = localPoint.y + halfBoard;
            int col = Mathf.FloorToInt(relX / step);
            int row = Mathf.FloorToInt(relY / step);
            return new Vector2Int(col, row);
        }

        /// <summary>
        /// 屏幕像素坐标 → 行列。封装 RectTransformUtility,这是 UGUI 标准做法。
        /// pressEventCamera 应传入对应 Canvas 的 worldCamera,
        /// ScreenSpaceOverlay 模式下传 null。
        /// </summary>
        public bool ScreenToCell(Vector2 screenPoint, Camera pressEventCamera, out Vector2Int cell, out Vector2 localPoint)
        {
            bool inside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _boardRoot, screenPoint, pressEventCamera, out localPoint);

            cell = LocalToCell(localPoint);
            return inside;
        }

        /// <summary>
        /// 给定一组方块单元的相对坐标(以左下角为原点)和原点行列,返回它们的屏幕居中世界局部位置(BoardRoot 局部坐标)。
        /// 用于"放置预览"或"吸附中"时,把方块移动到棋盘上对应位置。
        /// </summary>
        public Vector2 BlockOriginToLocal(int originCol, int originRow)
        {
            return CellToLocal(originCol, originRow);
        }

        public bool IsInside(int col, int row)
            => col >= 0 && col < Cols && row >= 0 && row < Rows;
    }
}
