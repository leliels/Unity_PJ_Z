// TODO: Sprite Atlas 打包脚本待修复 Unity 6 兼容性，暂时屏蔽
#if false  // ← 屏蔽开关，修复后改为 true 或删除此行
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using System;
using System.IO;

/// <summary>
/// 菜单：Tools → Create Digit Sprite Atlas
/// 一键将 Resources/Digits/ 下的 SH1/SH2 数字图片打包为 Sprite Atlas
///
/// 图集输出路径：Assets/Art/DigitSpritesAtlas.spriteatlas
/// </summary>
public static class CreateDigitAtlas
{
    [MenuItem("BlockPuzzle/AI 工具/Create Digit Sprite Atlas", false, 201)]
    public static void Execute()
    {
        string atlasPath = "Assets/Art/DigitSpritesAtlas.spriteatlas";
        string digitsFolder = "Assets/Resources/Digits";

        // 确保目录存在
        string dir = Path.GetDirectoryName(atlasPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // 如果已存在则删除重建
        if (File.Exists(atlasPath))
            AssetDatabase.DeleteAsset(atlasPath);

        // 创建 Sprite Atlas
        var atlas = ScriptableObject.CreateInstance<SpriteAtlas>();

        // 使用 SerializedObject 方式安全地设置属性（兼容 Unity 6）
        var so = new SerializedObject(atlas);

        // 配置 PackingSettings
        var packing = so.FindProperty("m_PackingSettings");
        if (packing != null)
        {
            packing.FindPropertyRelative("m_BlockSize").intValue = 2048;
            packing.FindPropertyRelative("m_BlockOffset").intValue = 1;
            packing.FindPropertyRelative("m_EnableRotation").boolValue = false;
            packing.FindPropertyRelative("m_EnableTightPacking").boolValue = true;
            packing.FindPropertyRelative("m_Padding").intValue = 2;
        }

        // 配置 TextureSettings
        var texture = so.FindProperty("m_TextureSettings");
        if (texture != null)
        {
            texture.FindPropertyRelative("m_Readable").boolValue = false;
            texture.FindPropertyRelative("m_GenerateMipMaps").boolValue = false;
            texture.FindPropertyRelative("m_sRGB").boolValue = true;
            texture.FindPropertyRelative("m_FilterMode").enumValueIndex = (int)FilterMode.Bilinear;
        }

        so.ApplyModifiedProperties();

        // 添加 SH1 + SH2 共 20 张数字图片到打包列表
        using (var list = new SerializedList(so.FindProperty("m_Packables")))
        {
            list.Clear();
            for (int i = 0; i < 10; i++)
            {
                AddSprite(list, $"{digitsFolder}/SH1_{i}.png");
                AddSprite(list, $"{digitsFolder}/SH2_{i}.png");
            }
        }

        so.ApplyModifiedProperties();

        // 保存资源
        AssetDatabase.CreateAsset(atlas, atlasPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CreateDigitAtlas] 图集创建成功！\n" +
                  $"路径: {atlasPath}\n" +
                  $"包含: SH1_0~9 (黄色) + SH2_0~9 (白色) 共 20 张\n\n" +
                  $"提示: 如需重新打包，再次点击菜单 Tools → Create Digit Sprite Atlas 即可");

        // 在 Project 窗口中选中生成的图集
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(atlasPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    /// <summary>
    /// 安全地向 SerializedProperty 列表中添加一个 Object 引用
    /// </summary>
    private static void AddSprite(SerializedList list, string assetPath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            Debug.LogWarning($"[CreateDigitAtlas] 未找到: {assetPath}");
            return;
        }
        list.arraySize++;
        list.GetElementAt(list.arraySize - 1).objectReferenceValue = sprite;
    }

    /// <summary>
    /// 辅助类：封装 SerializedProperty 数组操作
    /// </summary>
    private sealed class SerializedList : IDisposable
    {
        private readonly SerializedProperty _prop;
        public int arraySize { get => _prop.arraySize; set => _prop.arraySize = value; }
        public SerializedList(SerializedProperty prop) { _prop = prop; }
        public void Clear() => _prop.ClearArray();
        public SerializedProperty GetElementAt(int index) => _prop.GetArrayElementAtIndex(index);
        public void Dispose() { /* nothing to dispose */ }
    }
}
#endif
#endif  // ← 对应 #if false 屏蔽开关

