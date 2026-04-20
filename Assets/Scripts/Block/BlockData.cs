using UnityEngine;
using System.Collections.Generic;

namespace BlockPuzzle.Block
{
    /// <summary>
    /// 方块形状数据定义
    /// </summary>
    [System.Serializable]
    public class BlockData
    {
        public string ShapeName;
        public Vector2Int[] Cells;  // 相对坐标（以左下角为原点）
        public int Weight;          // 生成权重

        public BlockData(string name, Vector2Int[] cells, int weight = 10)
        {
            ShapeName = name;
            Cells = cells;
            Weight = weight;
        }

        /// <summary>
        /// 获取所有预定义方块形状
        /// </summary>
        public static List<BlockData> GetAllShapes()
        {
            return new List<BlockData>
            {
                // 1格
                new BlockData("Dot", new[] {
                    new Vector2Int(0, 0)
                }, 5),

                // 2格 - 横
                new BlockData("H2", new[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0)
                }, 8),

                // 2格 - 竖
                new BlockData("V2", new[] {
                    new Vector2Int(0, 0), new Vector2Int(0, 1)
                }, 8),

                // 3格 - 横
                new BlockData("H3", new[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)
                }, 10),

                // 3格 - 竖
                new BlockData("V3", new[] {
                    new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)
                }, 10),

                // 3格 - L型
                new BlockData("L3", new[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1)
                }, 10),

                // 4格 - 横
                new BlockData("H4", new[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0)
                }, 6),

                // 4格 - 竖
                new BlockData("V4", new[] {
                    new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3)
                }, 6),

                // 4格 - 正方形
                new BlockData("Square", new[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)
                }, 8),

                // 4格 - T型
                new BlockData("T4", new[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1)
                }, 6),

                // 4格 - S型
                new BlockData("S4", new[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1)
                }, 6),

                // 4格 - Z型
                new BlockData("Z4", new[] {
                    new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)
                }, 6),

                // 4格 - L型
                new BlockData("L4", new[] {
                    new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0)
                }, 6),

                // 4格 - J型
                new BlockData("J4", new[] {
                    new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(0, 0)
                }, 6),

                // 5格 - 横
                new BlockData("H5", new[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(3, 0), new Vector2Int(4, 0)
                }, 3),

                // 5格 - 竖
                new BlockData("V5", new[] {
                    new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2),
                    new Vector2Int(0, 3), new Vector2Int(0, 4)
                }, 3),

                // 9格 - 3x3正方形
                new BlockData("BigSquare", new[] {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                    new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
                }, 2),

                // 5格 - 大L型
                new BlockData("BigL", new[] {
                    new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2),
                    new Vector2Int(1, 0), new Vector2Int(2, 0)
                }, 4),
            };
        }

        /// <summary>
        /// 按权重随机选择一个方块形状
        /// </summary>
        public static BlockData GetRandomShape()
        {
            var shapes = GetAllShapes();
            int totalWeight = 0;
            foreach (var s in shapes) totalWeight += s.Weight;

            int rand = Random.Range(0, totalWeight);
            int cumulative = 0;
            foreach (var s in shapes)
            {
                cumulative += s.Weight;
                if (rand < cumulative) return s;
            }
            return shapes[shapes.Count - 1];
        }
    }
}
