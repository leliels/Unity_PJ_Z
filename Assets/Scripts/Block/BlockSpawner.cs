using System;
using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Core;
using BlockPuzzle.Board;
using BlockPuzzle.Utils;

namespace BlockPuzzle.Block
{
    /// <summary>
    /// 方块生成器：管理候选区的方块生成和刷新
    /// </summary>
    public class BlockSpawner : Singleton<BlockSpawner>
    {
        /// <summary>所有候选方块用完后刷新事件</summary>
        public event Action OnCandidatesRefreshed;

        // 候选方块数据（null 表示已被使用）
        private BlockData[] _candidateData;
        // 候选方块 GameObject（null 表示已被使用）
        private GameObject[] _candidateObjects;
        // 使用计数
        private int _usedCount;

        /// <summary>
        /// 获取当前剩余候选方块数据列表（用于游戏结束判定）
        /// </summary>
        public List<BlockData> GetRemainingCandidates()
        {
            var list = new List<BlockData>();
            if (_candidateData == null) return list;
            for (int i = 0; i < _candidateData.Length; i++)
            {
                if (_candidateData[i] != null)
                    list.Add(_candidateData[i]);
            }
            return list;
        }

        /// <summary>
        /// 初始化候选区
        /// </summary>
        public void Init()
        {
            _candidateData = new BlockData[Constants.CandidateCount];
            _candidateObjects = new GameObject[Constants.CandidateCount];
            _usedCount = 0;

            SpawnCandidates();
        }

        /// <summary>
        /// 清除所有候选方块（重新开始用）
        /// </summary>
        public void ClearAll()
        {
            if (_candidateObjects != null)
            {
                for (int i = 0; i < _candidateObjects.Length; i++)
                {
                    if (_candidateObjects[i] != null)
                        Destroy(_candidateObjects[i]);
                }
            }
            _candidateData = null;
            _candidateObjects = null;
            _usedCount = 0;
        }

        /// <summary>
        /// 标记某个候选方块已被使用
        /// </summary>
        public void MarkUsed(int index)
        {
            if (index < 0 || index >= Constants.CandidateCount) return;

            _candidateData[index] = null;
            if (_candidateObjects[index] != null)
            {
                Destroy(_candidateObjects[index]);
                _candidateObjects[index] = null;
            }

            _usedCount++;

            // 3个都用完，刷新
            if (_usedCount >= Constants.CandidateCount)
            {
                _usedCount = 0;
                SpawnCandidates();
                OnCandidatesRefreshed?.Invoke();
            }
        }

        // ==================== 生成候选方块 ====================

        private void SpawnCandidates()
        {
            for (int i = 0; i < Constants.CandidateCount; i++)
            {
                // 随机选择方块形状
                var data = BlockData.GetRandomShape();
                _candidateData[i] = data;

                // 随机选择颜色
                int colorIndex = UnityEngine.Random.Range(0, Constants.BlockColors.Length);
                Color blockColor = Constants.BlockColors[colorIndex];

                // 计算位置（水平排列）
                float totalWidth = (Constants.CandidateCount - 1) * Constants.CandidateSpacing;
                float startX = Constants.CandidateCenter.x - totalWidth / 2f;
                Vector3 pos = new Vector3(startX + i * Constants.CandidateSpacing, Constants.CandidateCenter.y, 0f);

                // 创建方块 GameObject
                var blockGo = CreateBlockVisual(data, blockColor, pos, Constants.CandidateScale);
                blockGo.name = $"Candidate_{i}_{data.ShapeName}";

                // 添加拖拽组件
                var drag = blockGo.AddComponent<BlockDrag>();
                drag.Init(data, colorIndex, i);

                _candidateObjects[i] = blockGo;
            }
        }

        /// <summary>
        /// 创建方块的可视化 GameObject。
        /// 方块的锚点（transform.position）= cell(0,0) 的位置，
        /// 即形状的"左下角那一格"的中心。这样 BlockDrag 里
        /// transform.position 与 BoardManager.WorldToGrid 的语义完全对齐：
        /// 鼠标指向哪个格子，那个格子就是方块左下角的放置格。
        /// </summary>
        public static GameObject CreateBlockVisual(BlockData data, Color color, Vector3 position, float scale)
        {
            var root = new GameObject("Block");
            root.transform.position = position;

            // 计算形状的最小边界（相对于 cells 原点）
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var cell in data.Cells)
            {
                if (cell.x < minX) minX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y > maxY) maxY = cell.y;
            }

            float step = Constants.CellSize + Constants.CellSpacing;

            // cells 坐标一律归一到从 (0,0) 开始，锚点就是左下角那格
            foreach (var cell in data.Cells)
            {
                var cellGo = new GameObject($"BlockCell_{cell.x}_{cell.y}");
                cellGo.transform.SetParent(root.transform);

                float localX = (cell.x - minX) * step;
                float localY = (cell.y - minY) * step;
                cellGo.transform.localPosition = new Vector3(localX, localY, 0f);
                cellGo.transform.localScale = Vector3.one * Constants.CellSize;

                var sr = cellGo.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteUtils.WhiteSquare;
                sr.color = color;
                sr.sortingOrder = 10;
            }

            root.transform.localScale = Vector3.one * scale;

            // Collider2D 覆盖整个形状的包围盒
            int widthCells = maxX - minX + 1;
            int heightCells = maxY - minY + 1;
            var collider = root.AddComponent<BoxCollider2D>();
            // collider 的 offset 要放在包围盒中心（相对于锚点=左下角格子中心）
            collider.offset = new Vector2(
                (widthCells - 1) * step * 0.5f,
                (heightCells - 1) * step * 0.5f
            );
            collider.size = new Vector2(widthCells * step, heightCells * step);

            return root;
        }
    }
}
