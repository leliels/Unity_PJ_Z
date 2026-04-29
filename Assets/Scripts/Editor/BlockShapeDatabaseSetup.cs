#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BlockPuzzle.Block;

public static class BlockShapeDatabaseSetup
{
    private const string DefaultFolder = "Assets/Configs/BlockShapes";
    private const string BlockSpawnerPrefabPath = "Assets/Prefabs/Board/[BlockSpawner].prefab";

    [MenuItem("Tools/方块形状配置器/创建默认数据库并设为运行时使用", false, 241)]
    public static void CreateDefaultAndSetRuntime()
    {
        var database = CreateDefaultDatabaseIfNeeded();
        SetRuntimeDatabase(database);
        Selection.activeObject = database;
        Debug.Log($"[BlockShapeDatabaseSetup] 默认方块形状数据库已创建并设置为运行时使用：{AssetDatabase.GetAssetPath(database)}");
    }

    public static BlockShapeDatabase CreateDefaultDatabaseIfNeeded()
    {
        EnsureFolder("Assets/Configs");
        EnsureFolder(DefaultFolder);

        var database = AssetDatabase.LoadAssetAtPath<BlockShapeDatabase>(BlockShapeDatabase.DefaultAssetPath);
        if (database != null)
            return database;

        database = ScriptableObject.CreateInstance<BlockShapeDatabase>();
        database.ResetToDefaultShapes();
        AssetDatabase.CreateAsset(database, BlockShapeDatabase.DefaultAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    public static void SetRuntimeDatabase(BlockShapeDatabase database)
    {
        if (database == null)
            return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlockSpawnerPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[BlockShapeDatabaseSetup] 找不到 BlockSpawner Prefab：{BlockSpawnerPrefabPath}");
            return;
        }

        var spawner = prefab.GetComponent<BlockSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[BlockShapeDatabaseSetup] BlockSpawner Prefab 上没有 BlockSpawner 组件。");
            return;
        }

        var serializedObject = new SerializedObject(spawner);
        var property = serializedObject.FindProperty("_shapeDatabase");
        if (property == null)
        {
            Debug.LogWarning("[BlockShapeDatabaseSetup] BlockSpawner 尚未包含 _shapeDatabase 字段，请等待脚本编译完成后重试。");
            return;
        }

        property.objectReferenceValue = database;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
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
