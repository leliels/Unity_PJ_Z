#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BlockPuzzle.Block;
using BlockPuzzle.Utils;

public sealed class BlockShapeDatabaseEditorWindow : EditorWindow
{
    private const string WindowTitle = "方块形状配置器";
    private const string DefaultFolder = "Assets/Configs/BlockShapes";
    private const string BlockSpawnerPrefabPath = "Assets/Prefabs/Board/[BlockSpawner].prefab";

    private BlockShapeDatabase _database;
    private int _selectedIndex;
    private int _tabIndex;
    private int _browseColumns = 5;
    private string _searchText = string.Empty;
    private Vector2 _listScroll;
    private Vector2 _editScroll;
    private Vector2 _browseScroll;

    [MenuItem("BlockPuzzle/方块形状配置器", false, 10)]
    public static void Open()
    {
        var window = GetWindow<BlockShapeDatabaseEditorWindow>();
        window.titleContent = new GUIContent(WindowTitle);
        window.minSize = new Vector2(900f, 580f);
        window.Show();
    }

    private void OnEnable()
    {
        _database = LoadDefaultOrFirstDatabase();
        ClampSelectedIndex();
    }

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4f);

        if (_database == null)
        {
            EditorGUILayout.HelpBox("还没有加载方块形状数据库。可以新建默认数据库，或手动选择已有数据库。", MessageType.Info);
            if (GUILayout.Button("创建默认方块形状数据库", GUILayout.Height(32f)))
                _database = CreateDefaultDatabase();
            return;
        }

        _tabIndex = GUILayout.Toolbar(_tabIndex, new[] { "编辑形状", "浏览全部" });
        EditorGUILayout.Space(4f);

        if (_tabIndex == 0)
            DrawEditTab();
        else
            DrawBrowseTab();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.LabelField(WindowTitle, EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        _database = (BlockShapeDatabase)EditorGUILayout.ObjectField("Database", _database, typeof(BlockShapeDatabase), false);
        if (EditorGUI.EndChangeCheck())
            ClampSelectedIndex();

        if (GUILayout.Button("新建", GUILayout.Width(58f)))
            _database = CreateDatabaseViaSavePanel();
        if (GUILayout.Button("默认库", GUILayout.Width(70f)))
            _database = CreateDefaultDatabase();
        if (GUILayout.Button("保存", GUILayout.Width(58f)))
            SaveDatabase();
        if (GUILayout.Button("设为运行时使用", GUILayout.Width(118f)))
            SetAsRuntimeDatabase();
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(true))
        {
            string path = _database != null ? AssetDatabase.GetAssetPath(_database) : "未选择";
            EditorGUILayout.TextField("路径", path);
        }
    }

    private void DrawEditTab()
    {
        EditorGUILayout.BeginHorizontal();
        DrawShapeList(260f);
        DrawShapeEditor();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawShapeList(float width)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width));
        EditorGUILayout.LabelField("形状列表/小预览", EditorStyles.boldLabel);
        _searchText = EditorGUILayout.TextField("搜索", _searchText);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("新建")) AddNewShape();
        if (GUILayout.Button("复制")) DuplicateSelectedShape();
        if (GUILayout.Button("删除")) DeleteSelectedShape();
        EditorGUILayout.EndHorizontal();

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUI.skin.box);
        var shapes = _database.Shapes;
        for (int i = 0; i < shapes.Count; i++)
        {
            var shape = shapes[i];
            if (shape == null) continue;
            if (!MatchesSearch(shape)) continue;

            bool selected = i == _selectedIndex;
            var oldColor = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = new Color(0.45f, 0.65f, 1f, 1f);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            bool enabled = EditorGUILayout.Toggle(shape.Enabled, GUILayout.Width(18f));
            if (enabled != shape.Enabled)
            {
                Undo.RecordObject(_database, "Toggle Shape Enabled");
                shape.Enabled = enabled;
                MarkDirty();
            }

            if (GUILayout.Button($"{shape.Id}  w={shape.Weight}", EditorStyles.miniButton))
                _selectedIndex = i;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(GetShapePreviewText(shape), EditorStyles.miniLabel, GUILayout.Height(56f));
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = oldColor;
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawShapeEditor()
    {
        ClampSelectedIndex();
        EditorGUILayout.BeginVertical();

        if (_database.Shapes.Count == 0)
        {
            EditorGUILayout.HelpBox("当前数据库没有任何形状。", MessageType.Warning);
            if (GUILayout.Button("新建形状")) AddNewShape();
            EditorGUILayout.EndVertical();
            return;
        }

        var shape = _database.Shapes[_selectedIndex];
        if (shape == null)
        {
            EditorGUILayout.HelpBox("当前形状为空。", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        _editScroll = EditorGUILayout.BeginScrollView(_editScroll);
        EditorGUILayout.LabelField("当前形状编辑", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        string id = EditorGUILayout.TextField("ID", shape.Id);
        string displayName = EditorGUILayout.TextField("显示名", shape.DisplayName);
        bool enabled = EditorGUILayout.Toggle("启用", shape.Enabled);
        int weight = EditorGUILayout.IntField("权重", shape.Weight);
        string note = EditorGUILayout.TextField("备注", shape.Note);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_database, "Edit Shape Info");
            shape.Id = id;
            shape.DisplayName = displayName;
            shape.Enabled = enabled;
            shape.Weight = weight;
            shape.Note = note;
            MarkDirty();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("编辑网格", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        int cols = EditorGUILayout.IntField("列", _database.EditorGridColumns, GUILayout.MaxWidth(180f));
        int rows = EditorGUILayout.IntField("行", _database.EditorGridRows, GUILayout.MaxWidth(180f));
        if (GUILayout.Button("应用尺寸", GUILayout.Width(90f)))
        {
            Undo.RecordObject(_database, "Resize Edit Grid");
            _database.EditorGridColumns = cols;
            _database.EditorGridRows = rows;
            MarkDirty();
        }
        EditorGUILayout.EndHorizontal();

        DrawCellGrid(shape);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("清空"))
        {
            Undo.RecordObject(_database, "Clear Shape Cells");
            shape.SetCells(new Vector2Int[0], false);
            MarkDirty();
        }
        if (GUILayout.Button("规格化到左下角"))
        {
            Undo.RecordObject(_database, "Normalize Shape Cells");
            shape.NormalizeToOrigin();
            MarkDirty();
        }
        if (GUILayout.Button("生成4方向"))
            GenerateFourDirections(shape);
        EditorGUILayout.EndHorizontal();

        DrawValidation(shape);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawCellGrid(BlockShapeDefinition shape)
    {
        var cells = new HashSet<Vector2Int>(shape.Cells);
        int cols = _database.EditorGridColumns;
        int rows = _database.EditorGridRows;

        EditorGUILayout.Space(4f);
        for (int y = rows - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(y.ToString(), GUILayout.Width(18f));
            for (int x = 0; x < cols; x++)
            {
                var cell = new Vector2Int(x, y);
                bool filled = cells.Contains(cell);
                if (GUILayout.Button(filled ? "■" : "□", GUILayout.Width(30f), GUILayout.Height(28f)))
                {
                    Undo.RecordObject(_database, "Toggle Shape Cell");
                    if (filled) cells.Remove(cell); else cells.Add(cell);
                    shape.SetCells(cells, false);
                    MarkDirty();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(18f);
        for (int x = 0; x < cols; x++)
            GUILayout.Label(x.ToString(), GUILayout.Width(30f));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBrowseTab()
    {
        EditorGUILayout.BeginHorizontal();
        _searchText = EditorGUILayout.TextField("搜索", _searchText);
        _browseColumns = EditorGUILayout.IntSlider("每行列数", _browseColumns, 2, 8);
        EditorGUILayout.EndHorizontal();

        _browseScroll = EditorGUILayout.BeginScrollView(_browseScroll);
        var visibleShapes = _database.Shapes.Where(s => s != null && MatchesSearch(s)).ToList();
        for (int i = 0; i < visibleShapes.Count; i += _browseColumns)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < _browseColumns && i + j < visibleShapes.Count; j++)
                DrawBrowseCard(visibleShapes[i + j]);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawBrowseCard(BlockShapeDefinition shape)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(150f), GUILayout.Height(140f));
        EditorGUILayout.BeginHorizontal();
        bool enabled = EditorGUILayout.Toggle(shape.Enabled, GUILayout.Width(18f));
        if (enabled != shape.Enabled)
        {
            Undo.RecordObject(_database, "Toggle Shape Enabled");
            shape.Enabled = enabled;
            MarkDirty();
        }
        EditorGUILayout.LabelField(shape.Id, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField($"w={shape.Weight}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField(GetShapePreviewText(shape), EditorStyles.miniLabel, GUILayout.Height(70f));
        if (GUILayout.Button("编辑"))
        {
            _selectedIndex = _database.Shapes.IndexOf(shape);
            _tabIndex = 0;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawValidation(BlockShapeDefinition shape)
    {
        var (errors, warnings) = ValidateShape(shape);
        MessageType type = errors.Count > 0 ? MessageType.Error : warnings.Count > 0 ? MessageType.Warning : MessageType.Info;
        var lines = new List<string>();
        lines.Add(errors.Count == 0 ? "错误: 无" : "错误:\n- " + string.Join("\n- ", errors));
        lines.Add(warnings.Count == 0 ? "警告: 无" : "警告:\n- " + string.Join("\n- ", warnings));
        EditorGUILayout.HelpBox(string.Join("\n", lines), type);
    }

    private (List<string> errors, List<string> warnings) ValidateShape(BlockShapeDefinition shape)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var cells = shape.CellsArray;

        if (cells.Length == 0)
        {
            errors.Add("当前形状没有任何格子。");
            return (errors, warnings);
        }

        GetBounds(cells, out _, out _, out int width, out int height);
        if (width > Constants.BoardCols || height > Constants.BoardRows)
            errors.Add($"当前形状尺寸 {width}×{height}，无法放入 {Constants.BoardCols}×{Constants.BoardRows} 棋盘。");
        if (width > 4)
            warnings.Add("当前形状宽度超过 4。当前规则允许，但需要确认体验。 ");
        if (!IsConnected(cells))
            warnings.Add("当前形状不连通。当前规则允许，但需要确认体验。 ");
        if (shape.Weight <= 0)
            warnings.Add("当前形状权重为 0，不会参与随机。 ");

        return (errors, warnings);
    }

    private void AddNewShape()
    {
        Undo.RecordObject(_database, "Add Shape");
        string id = MakeUniqueId("NewShape");
        _database.Shapes.Add(new BlockShapeDefinition(id, "新方块", new[] { new Vector2Int(0, 0) }, 10));
        _selectedIndex = _database.Shapes.Count - 1;
        MarkDirty();
    }

    private void DuplicateSelectedShape()
    {
        if (_database.Shapes.Count == 0) return;
        Undo.RecordObject(_database, "Duplicate Shape");
        var clone = _database.Shapes[_selectedIndex].Clone(MakeUniqueId(_database.Shapes[_selectedIndex].Id + "_Copy"));
        _database.Shapes.Add(clone);
        _selectedIndex = _database.Shapes.Count - 1;
        MarkDirty();
    }

    private void DeleteSelectedShape()
    {
        if (_database.Shapes.Count == 0) return;
        if (!EditorUtility.DisplayDialog("删除形状", $"确定删除 {_database.Shapes[_selectedIndex].Id} 吗？", "删除", "取消")) return;

        Undo.RecordObject(_database, "Delete Shape");
        _database.Shapes.RemoveAt(_selectedIndex);
        ClampSelectedIndex();
        MarkDirty();
    }

    private void GenerateFourDirections(BlockShapeDefinition source)
    {
        if (source == null || !source.HasCells) return;

        Undo.RecordObject(_database, "Generate Four Directions");
        string baseId = StripRotationSuffix(source.Id);
        source.Id = MakeUniqueId(baseId + "_0", source);
        source.NormalizeToOrigin();

        AddRotatedShape(source, baseId, 90);
        AddRotatedShape(source, baseId, 180);
        AddRotatedShape(source, baseId, 270);
        MarkDirty();
    }

    private void AddRotatedShape(BlockShapeDefinition source, string baseId, int degrees)
    {
        var rotated = RotateCells(source.CellsArray, degrees);
        var shape = new BlockShapeDefinition(MakeUniqueId($"{baseId}_{degrees}"), $"{source.DisplayName}_{degrees}", rotated, source.Weight, source.Enabled)
        {
            Note = source.Note
        };
        _database.Shapes.Add(shape);
    }

    private static Vector2Int[] RotateCells(IEnumerable<Vector2Int> cells, int degrees)
    {
        var result = new List<Vector2Int>();
        foreach (var cell in cells)
        {
            Vector2Int rotated = degrees switch
            {
                90 => new Vector2Int(-cell.y, cell.x),
                180 => new Vector2Int(-cell.x, -cell.y),
                270 => new Vector2Int(cell.y, -cell.x),
                _ => cell
            };
            result.Add(rotated);
        }
        return BlockShapeDefinition.NormalizeCells(result);
    }

    private string MakeUniqueId(string baseId, BlockShapeDefinition ignore = null)
    {
        baseId = string.IsNullOrWhiteSpace(baseId) ? "Shape" : baseId.Trim();
        string id = baseId;
        int suffix = 1;
        while (_database != null && _database.Shapes.Any(s => s != null && s != ignore && s.Id == id))
            id = baseId + "_" + suffix++;
        return id;
    }

    private static string StripRotationSuffix(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "Shape";
        foreach (string suffix in new[] { "_0", "_90", "_180", "_270" })
        {
            if (id.EndsWith(suffix))
                return id.Substring(0, id.Length - suffix.Length);
        }
        return id;
    }

    private string GetShapePreviewText(BlockShapeDefinition shape)
    {
        var cells = shape.CellsArray;
        if (cells.Length == 0) return "(空)";

        GetBounds(cells, out int minX, out int minY, out int width, out int height);
        var set = new HashSet<Vector2Int>(cells);
        var lines = new List<string>();
        for (int y = height - 1; y >= 0; y--)
        {
            var line = string.Empty;
            for (int x = 0; x < width; x++)
                line += set.Contains(new Vector2Int(minX + x, minY + y)) ? "■" : "□";
            lines.Add(line);
        }
        return string.Join("\n", lines);
    }

    private static void GetBounds(Vector2Int[] cells, out int minX, out int minY, out int width, out int height)
    {
        minX = cells.Min(c => c.x);
        minY = cells.Min(c => c.y);
        int maxX = cells.Max(c => c.x);
        int maxY = cells.Max(c => c.y);
        width = maxX - minX + 1;
        height = maxY - minY + 1;
    }

    private static bool IsConnected(Vector2Int[] cells)
    {
        if (cells.Length <= 1) return true;
        var all = new HashSet<Vector2Int>(cells);
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(cells[0]);
        visited.Add(cells[0]);

        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var dir in dirs)
            {
                var next = current + dir;
                if (!all.Contains(next) || !visited.Add(next)) continue;
                queue.Enqueue(next);
            }
        }
        return visited.Count == all.Count;
    }

    private bool MatchesSearch(BlockShapeDefinition shape)
    {
        if (string.IsNullOrWhiteSpace(_searchText)) return true;
        return shape.Id.Contains(_searchText) || shape.DisplayName.Contains(_searchText);
    }

    private void ClampSelectedIndex()
    {
        if (_database == null || _database.Shapes.Count == 0)
        {
            _selectedIndex = 0;
            return;
        }
        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _database.Shapes.Count - 1);
    }

    private void MarkDirty()
    {
        if (_database == null) return;
        EditorUtility.SetDirty(_database);
    }

    private void SaveDatabase()
    {
        if (_database == null) return;
        MarkDirty();
        AssetDatabase.SaveAssetIfDirty(_database);
        AssetDatabase.SaveAssets();
    }

    private BlockShapeDatabase LoadDefaultOrFirstDatabase()
    {
        var defaultDb = AssetDatabase.LoadAssetAtPath<BlockShapeDatabase>(BlockShapeDatabase.DefaultAssetPath);
        if (defaultDb != null) return defaultDb;

        string[] guids = AssetDatabase.FindAssets("t:BlockShapeDatabase");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<BlockShapeDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private BlockShapeDatabase CreateDefaultDatabase()
    {
        EnsureFolder("Assets/Configs");
        EnsureFolder(DefaultFolder);

        var existing = AssetDatabase.LoadAssetAtPath<BlockShapeDatabase>(BlockShapeDatabase.DefaultAssetPath);
        if (existing != null) return existing;

        var db = CreateInstance<BlockShapeDatabase>();
        db.ResetToDefaultShapes();
        AssetDatabase.CreateAsset(db, BlockShapeDatabase.DefaultAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = db;
        return db;
    }

    private BlockShapeDatabase CreateDatabaseViaSavePanel()
    {
        EnsureFolder("Assets/Configs");
        EnsureFolder(DefaultFolder);

        string path = EditorUtility.SaveFilePanelInProject("新建方块形状数据库", "NewBlockShapeDatabase", "asset", "选择保存位置", DefaultFolder);
        if (string.IsNullOrEmpty(path)) return _database;

        var db = CreateInstance<BlockShapeDatabase>();
        db.ResetToDefaultShapes();
        db.DatabaseId = System.IO.Path.GetFileNameWithoutExtension(path);
        db.DisplayName = db.DatabaseId;
        AssetDatabase.CreateAsset(db, AssetDatabase.GenerateUniqueAssetPath(path));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = db;
        return db;
    }

    private void SetAsRuntimeDatabase()
    {
        if (_database == null) return;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlockSpawnerPrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("设置失败", $"找不到 BlockSpawner Prefab：{BlockSpawnerPrefabPath}", "OK");
            return;
        }

        var spawner = prefab.GetComponent<BlockSpawner>();
        if (spawner == null)
        {
            EditorUtility.DisplayDialog("设置失败", "Prefab 上没有 BlockSpawner 组件。", "OK");
            return;
        }

        var serializedObject = new SerializedObject(spawner);
        var property = serializedObject.FindProperty("_shapeDatabase");
        if (property == null)
        {
            EditorUtility.DisplayDialog("设置失败", "BlockSpawner 尚未包含 _shapeDatabase 字段，请等待脚本编译完成后重试。", "OK");
            return;
        }

        property.objectReferenceValue = _database;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("设置完成", "当前数据库已设置为运行时使用。", "OK");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif
