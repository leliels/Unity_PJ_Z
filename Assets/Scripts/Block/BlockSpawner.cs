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
        // --- Prefab 引用（Inspector 可配置） ---
        [Header("Prefab 配置")]
        [Tooltip("方块单格 Prefab（需含 SpriteRenderer）。为空时使用代码创建 fallback。")]
        [SerializeField] private GameObject _blockCellPrefab;

        /// <summary>所有候选方块用完后刷新事件</summary>
        public event Action OnCandidatesRefreshed;

        /// <summary>外部设置 Block Cell Prefab（供 SceneBootstrap 代码注入）</summary>
        public void SetBlockCellPrefab(GameObject prefab) { if (_blockCellPrefab == null) _blockCellPrefab = prefab; }

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

        // ==================== 运行时重新布局 ====================

        /// <summary>
        /// 运行时就地重新布局：根据当前 Constants 中的值重新计算候选方块位置和缩放。
        /// 不销毁/重建对象，保留方块数据和状态。
        /// </summary>
        public void RelayoutCandidates()
        {
            if (_candidateObjects == null || _candidateData == null) return;

            // 锚点等间距排列
            float totalWidth = (Constants.CandidateCount - 1) * Constants.CandidateSpacing;
            float startX = Constants.CandidateCenter.x - totalWidth / 2f;

            for (int i = 0; i < Constants.CandidateCount; i++)
            {
                var go = _candidateObjects[i];
                if (go == null) continue;

                Vector3 newPos = new Vector3(startX + i * Constants.CandidateSpacing, Constants.CandidateCenter.y, 0f);
                go.transform.position = newPos;
                go.transform.localScale = Vector3.one * Constants.CandidateScale;

                // 同步更新子格子的间距和大小
                RelayoutBlockCells(go, _candidateData[i]);

                var drag = go.GetComponent<BlockDrag>();
                if (drag != null)
                    drag.UpdateOriginalPosition(newPos, Vector3.one * Constants.CandidateScale);
            }
        }

        /// <summary>
        /// 重新排列方块内部格子的 localPosition（CellSize/CellSpacing 变化时）
        /// 以包围盒中心为原点，与 CreateBlockVisual 逻辑一致。
        /// </summary>
        private void RelayoutBlockCells(GameObject blockGo, BlockData data)
        {
            if (data == null || blockGo == null) return;

            float step = Constants.CellSize + Constants.CellSpacing;
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var cell in data.Cells)
            {
                if (cell.x < minX) minX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y > maxY) maxY = cell.y;
            }

            float centerOffsetX = (maxX - minX) * step * 0.5f;
            float centerOffsetY = (maxY - minY) * step * 0.5f;

            int childIdx = 0;
            foreach (var cell in data.Cells)
            {
                if (childIdx >= blockGo.transform.childCount) break;
                var child = blockGo.transform.GetChild(childIdx);
                child.localPosition = new Vector3(
                    (cell.x - minX) * step - centerOffsetX,
                    (cell.y - minY) * step - centerOffsetY,
                    0f
                );
                child.localScale = Vector3.one * Constants.CellSize;
                childIdx++;
            }
        }

        // ==================== 生成候选方块 ====================

        private void SpawnCandidates()
        {
            // 锚点等间距排列：CandidateSpacing 控制每个方块坐标轴之间的距离
            float totalWidth = (Constants.CandidateCount - 1) * Constants.CandidateSpacing;
            float startX = Constants.CandidateCenter.x - totalWidth / 2f;

            for (int i = 0; i < Constants.CandidateCount; i++)
            {
                var data = BlockData.GetRandomShape();
                _candidateData[i] = data;

                int colorIndex = UnityEngine.Random.Range(0, Constants.BlockColors.Length);
                Color blockColor = Constants.BlockColors[colorIndex];

                Vector3 pos = new Vector3(startX + i * Constants.CandidateSpacing, Constants.CandidateCenter.y, 0f);

                var blockGo = CreateBlockVisual(data, blockColor, pos, Constants.CandidateScale, _blockCellPrefab);
                blockGo.name = $"Candidate_{i}_{data.ShapeName}";

                var drag = blockGo.AddComponent<BlockDrag>();
                drag.Init(data, colorIndex, i);

                _candidateObjects[i] = blockGo;
            }
        }

        /// <summary>
        /// 创建方块的可视化 GameObject。
        /// 方块的锚点（transform.position）= 形状包围盒的视觉中心。
        /// 候选区显示时以中心对齐到固定坐标，视觉上居中。
        /// BlockDrag 拖拽时通过 AnchorOffset 补偿回左下角格子坐标，
        /// 确保 WorldToGrid 放置逻辑正确。
        /// </summary>
        /// <param name="cellPrefab">方块单格 Prefab，为 null 时代码创建 fallback</param>
        public static GameObject CreateBlockVisual(BlockData data, Color color, Vector3 position, float scale, GameObject cellPrefab = null)
        {
            var root = new GameObject("Block");
            root.transform.position = position;

            // 计算形状的最小/最大边界
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

            // 包围盒中心偏移（local 空间，未缩放）
            float centerOffsetX = (maxX - minX) * step * 0.5f;
            float centerOffsetY = (maxY - minY) * step * 0.5f;

            // 子格子以包围盒中心为原点排列
            foreach (var cell in data.Cells)
            {
                float localX = (cell.x - minX) * step - centerOffsetX;
                float localY = (cell.y - minY) * step - centerOffsetY;

                GameObject cellGo;
                SpriteRenderer sr;

                if (cellPrefab != null)
                {
                    cellGo = UnityEngine.Object.Instantiate(cellPrefab, root.transform);
                    cellGo.name = $"BlockCell_{cell.x}_{cell.y}";
                    cellGo.transform.localPosition = new Vector3(localX, localY, 0f);
                    cellGo.transform.localScale = Vector3.one * Constants.CellSize;
                    sr = cellGo.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = cellGo.AddComponent<SpriteRenderer>();
                }
                else
                {
                    cellGo = new GameObject($"BlockCell_{cell.x}_{cell.y}");
                    cellGo.transform.SetParent(root.transform);
                    cellGo.transform.localPosition = new Vector3(localX, localY, 0f);
                    cellGo.transform.localScale = Vector3.one * Constants.CellSize;
                    sr = cellGo.AddComponent<SpriteRenderer>();
                    sr.sprite = SpriteUtils.BlockSprite;
                }

                sr.color = color;
                sr.sortingOrder = 10;
            }

            root.transform.localScale = Vector3.one * scale;

            // Collider2D 覆盖整个形状的包围盒（以中心为原点，offset=0）
            int widthCells = maxX - minX + 1;
            int heightCells = maxY - minY + 1;
            var collider = root.AddComponent<BoxCollider2D>();
            collider.offset = Vector2.zero;
            collider.size = new Vector2(widthCells * step, heightCells * step);

            return root;
        }
    }
}
