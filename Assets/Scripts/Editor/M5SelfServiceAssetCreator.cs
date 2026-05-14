using UnityEngine;
using UnityEditor;
using BlockPuzzle.Config;

namespace BlockPuzzle.EditorTools
{
    /// <summary>
    /// 一键创建 M-R5 自助体系的 3 个配置 .asset(FxLibrary / FloatingTextLibrary / AudioBindings)。
    /// 已存在则跳过,不覆盖现有配置。
    /// </summary>
    public static class M5SelfServiceAssetCreator
    {
        [MenuItem("BlockPuzzle/AI 工具/创建自助体系配置 (FxLibrary 等)", priority = 50)]
        public static void CreateSelfServiceAssets()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Configs");

            CreateIfMissing<FxLibrary>("Assets/Resources/Configs/02_Feel/FxLibrary.asset");
            CreateIfMissing<FloatingTextLibrary>("Assets/Resources/Configs/02_Feel/FloatingTextLibrary.asset");
            CreateIfMissing<AudioBindings>("Assets/Resources/Configs/02_Feel/AudioBindings.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[自助体系] 已确保 FxLibrary / FloatingTextLibrary / AudioBindings 三份配置存在(为空时美术/策划可自由添加条目)。");
        }

        private static T CreateIfMissing<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
