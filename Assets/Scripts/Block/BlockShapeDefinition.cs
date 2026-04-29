using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle.Block
{
    /// <summary>
    /// 单个方块形状配置项。运行时会转换为 BlockData 使用。
    /// </summary>
    [Serializable]
    public sealed class BlockShapeDefinition
    {
        [SerializeField] private string _id = "NewShape";
        [SerializeField] private string _displayName = "新方块";
        [SerializeField] private bool _enabled = true;
        [SerializeField, Min(0)] private int _weight = 10;
        [SerializeField] private List<Vector2Int> _cells = new List<Vector2Int> { new Vector2Int(0, 0) };
        [SerializeField, TextArea(1, 3)] private string _note;

        public string Id
        {
            get => _id;
            set => _id = string.IsNullOrWhiteSpace(value) ? "NewShape" : value.Trim();
        }

        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value ?? string.Empty;
        }

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public int Weight
        {
            get => Mathf.Max(0, _weight);
            set => _weight = Mathf.Max(0, value);
        }

        public string Note
        {
            get => _note;
            set => _note = value ?? string.Empty;
        }

        public IReadOnlyList<Vector2Int> Cells
        {
            get
            {
                _cells ??= new List<Vector2Int>();
                return _cells;
            }
        }

        public Vector2Int[] CellsArray => _cells != null ? _cells.ToArray() : Array.Empty<Vector2Int>();

        public bool HasCells => _cells != null && _cells.Count > 0;

        public BlockShapeDefinition() { }

        public BlockShapeDefinition(string id, string displayName, IEnumerable<Vector2Int> cells, int weight, bool enabled = true)
        {
            Id = id;
            DisplayName = displayName;
            Enabled = enabled;
            Weight = weight;
            SetCells(cells, true);
        }

        public static BlockShapeDefinition FromBlockData(BlockData data)
        {
            if (data == null)
                return new BlockShapeDefinition();

            return new BlockShapeDefinition(data.ShapeName, data.ShapeName, data.Cells, data.Weight, true);
        }

        public BlockShapeDefinition Clone(string newId = null)
        {
            return new BlockShapeDefinition(
                string.IsNullOrWhiteSpace(newId) ? Id + "_Copy" : newId,
                DisplayName,
                CellsArray,
                Weight,
                Enabled)
            {
                Note = Note
            };
        }

        public BlockData ToBlockData()
        {
            return new BlockData(Id, NormalizeCells(CellsArray), Weight);
        }

        public void SetCells(IEnumerable<Vector2Int> cells, bool normalize)
        {
            _cells = new List<Vector2Int>();
            if (cells == null) return;

            var seen = new HashSet<Vector2Int>();
            foreach (var cell in cells)
            {
                if (seen.Add(cell))
                    _cells.Add(cell);
            }

            if (normalize)
                _cells = new List<Vector2Int>(NormalizeCells(_cells));
        }

        public void NormalizeToOrigin()
        {
            _cells = new List<Vector2Int>(NormalizeCells(_cells));
        }

        public static Vector2Int[] NormalizeCells(IEnumerable<Vector2Int> cells)
        {
            if (cells == null) return Array.Empty<Vector2Int>();

            var list = new List<Vector2Int>();
            int minX = int.MaxValue;
            int minY = int.MaxValue;

            foreach (var cell in cells)
            {
                list.Add(cell);
                if (cell.x < minX) minX = cell.x;
                if (cell.y < minY) minY = cell.y;
            }

            if (list.Count == 0) return Array.Empty<Vector2Int>();

            for (int i = 0; i < list.Count; i++)
                list[i] = new Vector2Int(list[i].x - minX, list[i].y - minY);

            list.Sort((a, b) => a.y == b.y ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return list.ToArray();
        }

#if UNITY_EDITOR
        public void EditorSanitize()
        {
            Id = _id;
            _displayName ??= string.Empty;
            _note ??= string.Empty;
            _weight = Mathf.Max(0, _weight);
            _cells ??= new List<Vector2Int>();

            var unique = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            foreach (var cell in _cells)
            {
                if (seen.Add(cell))
                    unique.Add(cell);
            }
            _cells = unique;
        }
#endif
    }
}
