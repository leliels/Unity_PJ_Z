using UnityEngine;
using UnityEditor;
using BlockPuzzle.Audio;
using BlockPuzzle.Block;
using BlockPuzzle.Config;
using BlockPuzzle.Mode;
using BlockPuzzle.Score;

namespace BlockPuzzle.EditorTools
{
    /// <summary>
    /// 给美术/策划用的"游戏配置中心"入口。
    ///
    /// 菜单 BlockPuzzle/游戏配置中心:
    ///   1. 找不到 GameConfig.asset 时,弹窗一键创建并自动填充现有 SO 引用
    ///   2. 找到 GameConfig.asset 时,直接选中(Inspector 显示自定义编辑器)
    ///
    /// 不包含具体字段绘制,具体字段绘制由 GameConfigInspector(CustomEditor)负责。
    /// </summary>
    public static class GameConfigShortcut
    {
        private const string MenuPath = "BlockPuzzle/游戏配置中心";
        private const string DefaultAssetPath = "Assets/Resources/Configs/GameConfig.asset";

        [MenuItem(MenuPath, priority = 0)]
        public static void OpenGameConfig()
        {
            var config = LoadOrCreate();
            if (config == null) return;

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        public static GameConfig LoadOrCreate()
        {
            // 1. 优先从默认路径加载
            var existing = AssetDatabase.LoadAssetAtPath<GameConfig>(DefaultAssetPath);
            if (existing != null) return existing;

            // 2. 全工程搜索
            var guids = AssetDatabase.FindAssets("t:" + nameof(GameConfig));
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            }

            // 3. 都没有,弹窗确认创建
            bool create = EditorUtility.DisplayDialog(
                "创建游戏总配置",
                "未找到 GameConfig.asset。\n\n是否在 Assets/Resources/Configs/GameConfig.asset 创建,并自动引用现有的子配置?",
                "创建",
                "取消");
            if (!create) return null;

            return CreateAndPopulate();
        }

        public static GameConfig CreateAndPopulate()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Configs");

            var config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, DefaultAssetPath);

            // 自动填充现有子 SO
            var so = new SerializedObject(config);
            TryAssign(so, "_score", FindSingleAsset<ScoreConfig>());
            TryAssign(so, "_shapes", FindSingleAsset<BlockShapeDatabase>());
            TryAssign(so, "_modeCatalog", FindSingleAsset<ModeCatalog>());
            TryAssign(so, "_audioLibrary", FindSingleAsset<AudioLibrary>());

            // 默认模式优先选 Mode_Traditional
            var defaultMode = FindAssetByName<GameModeConfig>("Mode_Traditional")
                              ?? FindSingleAsset<GameModeConfig>();
            TryAssign(so, "_defaultMode", defaultMode);

            // 同步创建 LayoutConfig / UIThemeConfig / GameplayTuning(M-R1 自带的三份新 SO)
            TryAssign(so, "_layout", CreateOrLoadAsset<LayoutConfig>("Assets/Resources/Configs/01_Gameplay/LayoutConfig.asset"));
            TryAssign(so, "_theme", CreateOrLoadAsset<UIThemeConfig>("Assets/Resources/Configs/02_Feel/UIThemeConfig.asset"));
            TryAssign(so, "_gameplay", CreateOrLoadAsset<GameplayTuning>("Assets/Resources/Configs/01_Gameplay/GameplayTuning.asset"));

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[游戏配置中心] 已创建 GameConfig: {DefaultAssetPath}\n请检查各子配置引用是否正确,需要时手动调整。");
            return config;
        }

        // ==================== 辅助 ====================

        private static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T FindSingleAsset<T>() where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids == null || guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static T FindAssetByName<T>(string name) where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"{name} t:{typeof(T).Name}");
            if (guids == null || guids.Length == 0) return null;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && asset.name == name) return asset;
            }
            return null;
        }

        private static void TryAssign(SerializedObject so, string fieldName, Object value)
        {
            if (value == null) return;
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.objectReferenceValue = value;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
