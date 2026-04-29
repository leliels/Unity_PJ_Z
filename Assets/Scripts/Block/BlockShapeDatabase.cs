using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle.Block
{
    /// <summary>
    /// 方块形状数据库。由编辑器窗口维护，运行时由 BlockSpawner 读取。
    /// </summary>
    [CreateAssetMenu(fileName = "DefaultBlockShapeDatabase", menuName = "BlockPuzzle/方块形状数据库")]
    public sealed class BlockShapeDatabase : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/Configs/BlockShapes/DefaultBlockShapeDatabase.asset";

        [SerializeField] private string _databaseId = "default";
        [SerializeField] private string _displayName = "默认方块库";
        [SerializeField, Min(1)] private int _editorGridColumns = 5;
        [SerializeField, Min(1)] private int _editorGridRows = 5;
        [SerializeField] private List<BlockShapeDefinition> _shapes = new List<BlockShapeDefinition>();
        [SerializeField, TextArea(2, 5)] private string _note;

        public string DatabaseId
        {
            get => _databaseId;
            set => _databaseId = string.IsNullOrWhiteSpace(value) ? "default" : value.Trim();
        }

        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value ?? string.Empty;
        }

        public int EditorGridColumns
        {
            get => Mathf.Clamp(_editorGridColumns, 1, 16);
            set => _editorGridColumns = Mathf.Clamp(value, 1, 16);
        }

        public int EditorGridRows
        {
            get => Mathf.Clamp(_editorGridRows, 1, 16);
            set => _editorGridRows = Mathf.Clamp(value, 1, 16);
        }

        public List<BlockShapeDefinition> Shapes
        {
            get
            {
                _shapes ??= new List<BlockShapeDefinition>();
                return _shapes;
            }
        }

        public string Note
        {
            get => _note;
            set => _note = value ?? string.Empty;
        }

        public bool TryGetRandomShape(out BlockData blockData)
        {
            blockData = null;
            var runtimeShapes = GetRuntimeShapes();
            if (runtimeShapes.Count == 0) return false;

            int totalWeight = 0;
            foreach (var shape in runtimeShapes)
                totalWeight += shape.Weight;

            if (totalWeight <= 0) return false;

            int rand = Random.Range(0, totalWeight);
            int cumulative = 0;
            foreach (var shape in runtimeShapes)
            {
                cumulative += shape.Weight;
                if (rand < cumulative)
                {
                    blockData = shape.ToBlockData();
                    return true;
                }
            }

            blockData = runtimeShapes[runtimeShapes.Count - 1].ToBlockData();
            return true;
        }

        public List<BlockShapeDefinition> GetRuntimeShapes()
        {
            var result = new List<BlockShapeDefinition>();
            foreach (var shape in Shapes)
            {
                if (shape == null) continue;
                if (!shape.Enabled) continue;
                if (shape.Weight <= 0) continue;
                if (!shape.HasCells) continue;
                result.Add(shape);
            }
            return result;
        }

        public void ResetToDefaultShapes()
        {
            Shapes.Clear();
            foreach (var blockData in BlockData.GetAllShapes())
                Shapes.Add(BlockShapeDefinition.FromBlockData(blockData));

            _databaseId = "default";
            _displayName = "默认方块库";
            _editorGridColumns = 5;
            _editorGridRows = 5;
            _note = "由方块形状配置器创建。";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            DatabaseId = _databaseId;
            _displayName ??= string.Empty;
            _note ??= string.Empty;
            _editorGridColumns = Mathf.Clamp(_editorGridColumns, 1, 16);
            _editorGridRows = Mathf.Clamp(_editorGridRows, 1, 16);
            _shapes ??= new List<BlockShapeDefinition>();

            foreach (var shape in _shapes)
                shape?.EditorSanitize();
        }
#endif
    }
}
