using System.Collections.Generic;
using BlockPuzzle.Utils;

namespace BlockPuzzle.Board
{
    /// <summary>
    /// 消除检测：检测并执行行/列消除
    /// </summary>
    public static class MatchChecker
    {
        /// <summary>
        /// 检测所有已填满的行和列
        /// </summary>
        /// <param name="grid">棋盘数据 [col, row]</param>
        /// <returns>需要消除的行索引列表和列索引列表</returns>
        public static (List<int> fullRows, List<int> fullCols) CheckMatches(bool[,] grid)
        {
            var fullRows = new List<int>();
            var fullCols = new List<int>();

            // 检查每一行
            for (int row = 0; row < Constants.BoardRows; row++)
            {
                bool full = true;
                for (int col = 0; col < Constants.BoardCols; col++)
                {
                    if (!grid[col, row])
                    {
                        full = false;
                        break;
                    }
                }
                if (full) fullRows.Add(row);
            }

            // 检查每一列
            for (int col = 0; col < Constants.BoardCols; col++)
            {
                bool full = true;
                for (int row = 0; row < Constants.BoardRows; row++)
                {
                    if (!grid[col, row])
                    {
                        full = false;
                        break;
                    }
                }
                if (full) fullCols.Add(col);
            }

            return (fullRows, fullCols);
        }

        /// <summary>
        /// 执行消除：清除指定行和列的所有格子
        /// </summary>
        /// <returns>被消除的格子坐标列表</returns>
        public static List<UnityEngine.Vector2Int> ClearLines(bool[,] grid, List<int> rows, List<int> cols)
        {
            var cleared = new List<UnityEngine.Vector2Int>();

            foreach (int row in rows)
            {
                for (int col = 0; col < Constants.BoardCols; col++)
                {
                    if (grid[col, row])
                    {
                        grid[col, row] = false;
                        cleared.Add(new UnityEngine.Vector2Int(col, row));
                    }
                }
            }

            foreach (int col in cols)
            {
                for (int row = 0; row < Constants.BoardRows; row++)
                {
                    if (grid[col, row])
                    {
                        grid[col, row] = false;
                        cleared.Add(new UnityEngine.Vector2Int(col, row));
                    }
                }
            }

            return cleared;
        }
    }
}
