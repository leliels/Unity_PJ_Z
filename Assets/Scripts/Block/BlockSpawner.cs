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

        [Header("候选区底板")]
        [Tooltip("候选槽位黑色底板 Sprite（DB_01.png）。无 Slot Prefab 时的 fallback。")]
        [SerializeField] private Sprite _candidateBoardSprite;

        [Tooltip("底板大小（世界单位）。值越大，底板越大。默认 4.5，建议范围 3.0~6.0。")]
        [SerializeField] private float _candidateBoardSize = 4.5f;

        /// <summary>设置候选区底板大小</summary>
        public void SetCandidateBoardSize(float size) { _candidateBoardSize = size; }

        [Tooltip("候选槽位 Prefab（可选）。Prefab 内名为 'BlockAnchor' 的子对象将作为方块挂载点。")]
        [SerializeField] private GameObject _candidateSlotPrefab;

        /// <summary>外部设置 Block Cell Prefab（供 SceneBootstrap 代码注入）</summary>
        public void SetBlockCellPrefab(GameObject prefab) { if (_blockCellPrefab == null) _blockCellPrefab = prefab; }

        /// <summary>设置候选区底板 Sprite</summary>
        public void SetCandidateBoardSprite(Sprite sprite) { if (_candidateBoardSprite == null) _candidateBoardSprite = sprite; }

        /// <summary>设置候选槽位 Prefab</summary>
        public void SetCandidateSlotPrefab(GameObject prefab) { if (_candidateSlotPrefab == null) _candidateSlotPrefab = prefab; }

        /// <summary>所有候选方块用完后刷新事件</summary>
        public event Action OnCandidatesRefreshed;

        // 候选方块数据（null 表示已被使用）
        private BlockData[] _candidateData;
        // 候选槽位 GameObject（Slot 容器 = 可拖拽的方块）
        private GameObject[] _candidateObjects;
        // 底板 GameObject（独立于 Slot，固定不动的背景板）
        private GameObject[] _candidateBoards;
        // 底板根容器（所有底板的统一父对象）
        private Transform _boardsContainer;
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
        /// 获取指定索引的候选方块世界坐标（供 BlockDrag 使用）
        /// </summary>
        public Vector3 GetCandidateWorldPosition(int index)
        {
            if (_candidateObjects == null || index < 0 || index >= _candidateObjects.Length)
                return Constants.CandidateCenter;
            var slot = _candidateObjects[index];
            if (slot == null) return Constants.CandidateCenter;
            return slot.transform.position;
        }

        /// <summary>
        /// 初始化候选区
        /// </summary>
        public void Init()
        {
            _candidateData = new BlockData[Constants.CandidateCount];
            _candidateObjects = new GameObject[Constants.CandidateCount];

            // 底板数组：保留已有底板引用（重新开始时复用），首次初始化则新建数组
            if (_candidateBoards == null)
                _candidateBoards = new GameObject[Constants.CandidateCount];

            _usedCount = 0;

            EnsureBoardsContainer();
            SpawnCandidates();
        }

        /// <summary>确保底板容器存在（懒创建，仅一次）</summary>
        private void EnsureBoardsContainer()
        {
            if (_boardsContainer != null && _boardsContainer.gameObject != null) return;
            var go = GameObject.Find("[CandidateBoards]");
            if (go == null)
                go = new GameObject("[CandidateBoards]");
            _boardsContainer = go.transform;
        }

        /// <summary>
        /// 清除所有候选方块（重新开始用）。
        /// 注意：底板(Board)不在此清除，底板一旦创建就持续存在。
        /// </summary>
        public void ClearAll()
        {
            if (_candidateObjects != null)
            {
                for (int i = 0; i < _candidateObjects.Length; i++)
                    if (_candidateObjects[i] != null) Destroy(_candidateObjects[i]);
            }
            _candidateData = null;
            _candidateObjects = null;
            // 注意：_candidateBoards 和 _boardsContainer 不清空，保持到底板存在
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
            // 底板保持不变，不随单个方块使用而销毁

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
                Vector3 newPos = new Vector3(startX + i * Constants.CandidateSpacing, Constants.CandidateCenter.y, 0f);

                // 更新 Slot（可拖拽的方块）位置和缩放
                var slot = _candidateObjects[i];
                if (slot != null)
                {
                    slot.transform.position = newPos;
                    slot.transform.localScale = Vector3.one * Constants.CandidateScale;
                }

                // 更新底板位置和大小（底板独立于 Slot，固定在背景上）
                if (_candidateBoards != null && _candidateBoards[i] != null)
                {
                    _candidateBoards[i].transform.position = newPos;
                    _candidateBoards[i].transform.localScale = new Vector3(_candidateBoardSize, _candidateBoardSize, 1f);
                }

                // 更新内部方块子格子的间距和大小
                // 兼容 Prefab 模式（BlockAnchor 子节点下）和 fallback 模式（直接在 Slot 下）
                Transform blockAnchor = slot.transform.Find("BlockAnchor") ?? slot.transform;
                var blockObj = blockAnchor.Find("Block");
                if (blockObj != null)
                {
                    RelayoutBlockCells(blockObj.gameObject, _candidateData[i]);
                    var drag = blockObj.GetComponent<BlockDrag>();
                    if (drag != null)
                        drag.UpdateOriginalPosition(newPos, Vector3.one * Constants.CandidateScale);
                }
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

                GameObject slotGo;
                Transform blockAnchor;

                if (_candidateSlotPrefab != null)
                {
                    // === Prefab 模式：美术可在 Prefab 内自由调整底板布局 ===
                    slotGo = Instantiate(_candidateSlotPrefab);
                    slotGo.SetActive(true);
                    slotGo.name = $"CandidateSlot_{i}";
                    slotGo.transform.position = pos;
                    slotGo.transform.localScale = Vector3.one * Constants.CandidateScale;

                    // 查找方块挂载点：优先找名为 "BlockAnchor" 的子对象，找不到就用根节点
                    var anchorTf = slotGo.transform.Find("BlockAnchor");
                    blockAnchor = anchorTf != null ? anchorTf : slotGo.transform;
                }
                else
                {
                    // === Fallback 模式：代码创建 Slot（仅方块），底板独立 ===
                    slotGo = new GameObject($"CandidateSlot_{i}");
                    slotGo.transform.position = pos;
                    slotGo.transform.localScale = Vector3.one * Constants.CandidateScale;

                    // 底板创建在固定容器中（不随 Slot 拖拽移动）
                    // 已有底板则复用（刷新时底板不变），仅更新位置和大小
                    if (_candidateBoardSprite != null)
                    {
                        var boardGo = _candidateBoards[i];
                        if (boardGo == null)
                        {
                            boardGo = new GameObject($"Board_{i}");
                            boardGo.transform.SetParent(_boardsContainer, false);

                            var sr = boardGo.AddComponent<SpriteRenderer>();
                            sr.sprite = _candidateBoardSprite;
                            sr.sortingOrder = 4;

                            _candidateBoards[i] = boardGo;
                        }
                        // 始终同步位置和大小（运行时调整参数时也需更新）
                        boardGo.transform.position = pos;
                        boardGo.transform.localScale = new Vector3(_candidateBoardSize, _candidateBoardSize, 1f);
                    }

                    blockAnchor = slotGo.transform;
                }

                // --- 方块视觉 ---
                var blockGo = CreateBlockVisual(data, blockColor, Vector3.zero, 1f, _blockCellPrefab);
                blockGo.transform.SetParent(blockAnchor, false);
                blockGo.name = "Block";

                // --- 将子对象 Block 的 Collider2D 复制到 Slot（供 BlockDrag 射线检测用） ---
                var blockCollider = blockGo.GetComponent<Collider2D>();
                if (blockCollider != null)
                {
                    var slotCollider = slotGo.GetComponent<BoxCollider2D>();
                    if (slotCollider == null)
                        slotCollider = slotGo.AddComponent<BoxCollider2D>();
                    // 复制碰撞体参数
                    if (blockCollider is BoxCollider2D box)
                    {
                        slotCollider.size = box.size;
                        slotCollider.offset = box.offset;
                    }
                    // 销毁 Block 子对象上的碰撞体，避免 Physics2D.OverlapPoint
                    // 命中子碰撞体而非 Slot 碰撞体，导致拖拽检测失败
                    Destroy(blockCollider);
                }

                // BlockDrag 挂在 Slot 上，操作整个槽位
                var drag = slotGo.AddComponent<BlockDrag>();
                drag.Init(data, colorIndex, i);

                _candidateObjects[i] = slotGo;
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
