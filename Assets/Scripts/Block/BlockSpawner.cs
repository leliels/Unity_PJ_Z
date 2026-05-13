using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Audio;
using BlockPuzzle.Board;
using BlockPuzzle.Config;
using BlockPuzzle.Core;
using BlockPuzzle.Utils;

namespace BlockPuzzle.Block
{
    /// <summary>
    /// 候选区生成器(UGUI 版,M-R3 重构)。
    ///
    /// 与旧版 SpriteRenderer 实现的区别:
    /// 1. 候选区是 PlayCanvas 下的 RectTransform,横排 N 个 Slot
    /// 2. 每个 Slot 是 UI 容器,内部生成 BlockCell Image
    /// 3. 拖拽改由 BlockDrag 用 IPointerDown / IDrag 接口,无需手动 Update + 射线检测
    /// 4. 不再有"底板 + Slot + BlockAnchor"三层嵌套,简化为 Slot/Block 两层
    /// </summary>
    public class BlockSpawner : Singleton<BlockSpawner>
    {
        // ==================== Inspector ====================
        [Header("UGUI 渲染")]
        [Tooltip("候选区根 RectTransform。挂在 PlayCanvas 下,通常是横排布局。")]
        [SerializeField] private RectTransform _candidateRoot;

        [Tooltip("单个候选槽位 Prefab(必须含 RectTransform + Image,Image 用作底板视觉)。")]
        [SerializeField] private GameObject _slotPrefab;

        [Tooltip("方块单格 Prefab(必须含 Image)。生成方块时由代码摆 N 个。")]
        [SerializeField] private GameObject _blockCellPrefab;

        [Header("数据源")]
        [Tooltip("方块形状库。为空时回退到代码内置形状。")]
        [SerializeField] private BlockShapeDatabase _shapeDatabase;

        public void SetShapeDatabase(BlockShapeDatabase db) { _shapeDatabase = db; }

        // ==================== 事件 ====================
        public event Action OnCandidatesRefreshed;

        // ==================== 内部状态 ====================
        private BlockData[] _candidateData;
        private GameObject[] _slotObjects;
        private GameObject[] _blockObjects;     // 每个 Slot 内部的"方块"子对象,挂 BlockDrag
        private int _usedCount;

        private LayoutConfig _layoutConfig;
        private UIThemeConfig _theme;
        private int _slotCount = 3;

        public RectTransform CandidateRoot => _candidateRoot;
        public LayoutConfig LayoutConfig => _layoutConfig;
        public UIThemeConfig Theme => _theme;
        public GameObject BlockCellPrefab => _blockCellPrefab;

        public void Configure(LayoutConfig layoutConfig, UIThemeConfig theme)
        {
            _layoutConfig = layoutConfig;
            _theme = theme;
            _slotCount = layoutConfig != null ? layoutConfig.CandidateSlotCount : Constants.CandidateCount;
        }

        // ==================== 公共接口 ====================

        public List<BlockData> GetRemainingCandidates()
        {
            var list = new List<BlockData>();
            if (_candidateData == null) return list;
            for (int i = 0; i < _candidateData.Length; i++)
                if (_candidateData[i] != null) list.Add(_candidateData[i]);
            return list;
        }

        public void Init()
        {
            if (_candidateRoot == null)
            {
                Debug.LogError("[BlockSpawner] _candidateRoot 未配置,无法初始化。");
                return;
            }

            ClearChildren();

            _candidateData = new BlockData[_slotCount];
            _slotObjects = new GameObject[_slotCount];
            _blockObjects = new GameObject[_slotCount];
            _usedCount = 0;

            EnsureLayout();
            CreateSlots();
            SpawnAllCandidates();
        }

        public void ClearAll()
        {
            ClearChildren();
            _candidateData = null;
            _slotObjects = null;
            _blockObjects = null;
            _usedCount = 0;
        }

        public void MarkUsed(int index)
        {
            if (index < 0 || index >= _slotCount) return;

            _candidateData[index] = null;
            if (_blockObjects[index] != null)
            {
                Destroy(_blockObjects[index]);
                _blockObjects[index] = null;
            }

            _usedCount++;

            if (_usedCount >= _slotCount)
            {
                _usedCount = 0;
                SpawnAllCandidates();
                OnCandidatesRefreshed?.Invoke();
            }
        }

        // ==================== 内部:槽位与方块生成 ====================

        private void ClearChildren()
        {
            if (_candidateRoot == null) return;
            for (int i = _candidateRoot.childCount - 1; i >= 0; i--)
            {
                var child = _candidateRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else
                {
#if UNITY_EDITOR
                    DestroyImmediate(child);
#endif
                }
            }
        }

        private void EnsureLayout()
        {
            // 候选区根用 HorizontalLayoutGroup 自动等距摆放,大小由 LayoutElement 决定
            var hlg = _candidateRoot.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = _candidateRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = _candidateRoot.rect.width * 0.04f;
        }

        private void CreateSlots()
        {
            float slotSize = ComputeSlotSize();
            for (int i = 0; i < _slotCount; i++)
            {
                GameObject slot;
                if (_slotPrefab != null)
                {
                    slot = Instantiate(_slotPrefab, _candidateRoot, false);
                    slot.name = $"CandidateSlot_{i}";
                }
                else
                {
                    slot = new GameObject($"CandidateSlot_{i}", typeof(RectTransform), typeof(Image));
                    slot.transform.SetParent(_candidateRoot, false);
                    var bgImg = slot.GetComponent<Image>();
                    bgImg.color = new Color(0f, 0f, 0f, 0.25f);
                    bgImg.raycastTarget = false;
                }

                var rt = slot.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(slotSize, slotSize);

                var le = slot.GetComponent<LayoutElement>();
                if (le == null) le = slot.AddComponent<LayoutElement>();
                le.preferredWidth = slotSize;
                le.preferredHeight = slotSize;

                _slotObjects[i] = slot;
            }
        }

        private float ComputeSlotSize()
        {
            // 候选槽 size = 棋盘单元格大小 * 4(因为最大形状是 3x3 + 一点 padding)
            // 这里不直接读 BoardManager,因为初始化顺序可能是 BlockSpawner 先于棋盘 Init。
            // 用候选区自身高度 * 0.95 作为槽位边长,UI 上更直观。
            float h = _candidateRoot.rect.height;
            if (h <= 0f) h = 200f;
            return h * 0.95f;
        }

        private void SpawnAllCandidates()
        {
            for (int i = 0; i < _slotCount; i++)
            {
                if (_slotObjects[i] == null) continue;
                var data = GetRandomShapeData();
                _candidateData[i] = data;

                int colorIndex = UnityEngine.Random.Range(0, GetBlockColors().Length);
                Color color = GetBlockColors()[colorIndex];

                var blockGo = CreateBlockInSlot(_slotObjects[i].transform, data, color, colorIndex, i);
                _blockObjects[i] = blockGo;
            }
        }

        private GameObject CreateBlockInSlot(Transform slotParent, BlockData data, Color color, int colorIndex, int slotIndex)
        {
            // 1. 一个根容器,持有 BlockDrag,大小撑满 Slot
            var blockGo = new GameObject("Block", typeof(RectTransform), typeof(CanvasGroup));
            var blockRt = blockGo.GetComponent<RectTransform>();
            blockRt.SetParent(slotParent, false);
            blockRt.anchorMin = Vector2.zero;
            blockRt.anchorMax = Vector2.one;
            blockRt.offsetMin = Vector2.zero;
            blockRt.offsetMax = Vector2.zero;
            blockRt.localScale = Vector3.one;

            // 一张透明 Image 作为整体的 raycast target,方便 PointerDown 事件命中整个槽位
            var hitImg = blockGo.AddComponent<Image>();
            hitImg.color = new Color(0f, 0f, 0f, 0f);
            hitImg.raycastTarget = true;

            // 2. 内部"几何容器",放各 BlockCell。它的尺寸 = 形状包围盒 * 单元尺寸。
            //    缩放在 Slot 中显示时按 BlockData 的实际占格数决定,不会撑出 Slot 边界。
            var contentGo = new GameObject("Cells", typeof(RectTransform));
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.SetParent(blockRt, false);
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);

            var (minX, minY, maxX, maxY) = GetShapeBounds(data);
            int widthCells = maxX - minX + 1;
            int heightCells = maxY - minY + 1;

            // 单格大小:**所有候选方块统一**(不再按形状大小变化),
            // 这样 1 格的方块和 5 格长条在视觉单格大小上完全一致,玩家更容易辨识。
            // 单格 = Slot 边长 / 5(留余地放下最大形状,如 5x5 的 BigSquare)。
            float slotEdge = blockRt.rect.width;
            if (slotEdge <= 0f) slotEdge = 200f;
            const int FixedReferenceCells = 5; // 固定参照格数,统一所有候选方块的单格视觉大小
            float cellSize = slotEdge * 0.85f / FixedReferenceCells;

            contentRt.sizeDelta = new Vector2(widthCells * cellSize, heightCells * cellSize);

            float originX = -widthCells * cellSize * 0.5f + cellSize * 0.5f;
            float originY = -heightCells * cellSize * 0.5f + cellSize * 0.5f;

            // 加载方块美术资源,与旧版一致(blk_base.png + 颜色 tint)
            var blockSprite = Utils.SpriteUtils.BlockSprite;

            foreach (var cell in data.Cells)
            {
                Image cellImg;
                if (_blockCellPrefab != null)
                {
                    var go = Instantiate(_blockCellPrefab, contentRt, false);
                    go.name = $"Cell_{cell.x}_{cell.y}";
                    cellImg = go.GetComponent<Image>();
                    if (cellImg == null) cellImg = go.AddComponent<Image>();
                }
                else
                {
                    var go = new GameObject($"Cell_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(contentRt, false);
                    cellImg = go.GetComponent<Image>();
                }
                cellImg.raycastTarget = false;
                // 用美术资源(blk_base.png),没有时再回退到纯色块
                if (cellImg.sprite == null && blockSprite != null) cellImg.sprite = blockSprite;
                cellImg.color = color;

                var rt = cellImg.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cellSize, cellSize);
                rt.anchoredPosition = new Vector2(
                    originX + (cell.x - minX) * cellSize,
                    originY + (cell.y - minY) * cellSize);
                rt.localScale = Vector3.one;
            }

            // 3. 挂 BlockDrag(UGUI 版),负责 Pointer 事件
            var drag = blockGo.AddComponent<BlockDrag>();
            drag.Init(data, colorIndex, color, slotIndex, this);

            // 4. 音效反馈
            if (blockGo.GetComponent<BlockAudioFeedback>() == null)
                blockGo.AddComponent<BlockAudioFeedback>();

            return blockGo;
        }

        private static (int minX, int minY, int maxX, int maxY) GetShapeBounds(BlockData data)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var c in data.Cells)
            {
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
            }
            return (minX, minY, maxX, maxY);
        }

        private BlockData GetRandomShapeData()
        {
            if (_shapeDatabase != null && _shapeDatabase.TryGetRandomShape(out var data))
                return data;
            return BlockData.GetRandomShape();
        }

        private Color[] GetBlockColors()
        {
            if (_theme != null && _theme.BlockColors != null && _theme.BlockColors.Length > 0)
                return _theme.BlockColors;
            return Constants.BlockColors;
        }
    }
}
