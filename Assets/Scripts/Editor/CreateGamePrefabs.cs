#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

/// <summary>
/// 菜单：Tools → 创建游戏 Prefab
/// 一键创建所有之前走代码 fallback 的 UI/游戏 Prefab
/// </summary>
public static class CreateGamePrefabs
{
    [MenuItem("BlockPuzzle/AI 工具/创建游戏 Prefab（一键生成）", false, 200)]
    public static void Execute()
    {
        EnsureFolder("Assets/Prefabs/UI");
        EnsureFolder("Assets/Prefabs/Board");

        CreateGameOverPanelPrefab();
        CreateScoreDisplayPrefab("ScoreDisplay", 32f, 56f, "Assets/Prefabs/UI/ScoreDisplay.prefab");
        CreateHighScoreDisplayPrefab();
        CreateCandidateSlotPrefab();
        CreateBlockSpawnerPrefab();
        CreateBackgroundCanvasPrefab();
        CreateCandidateBoardPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CreateGamePrefabs] 所有 Prefab 创建完成！\n" +
                  "- Assets/Prefabs/UI/GameOverPanel.prefab\n" +
                  "- Assets/Prefabs/UI/ScoreDisplay.prefab\n" +
                  "- Assets/Prefabs/UI/HighScoreDisplay.prefab\n" +
                  "- Assets/Prefabs/Board/CandidateSlot.prefab（已存在则跳过）\n" +
                  "- Assets/Prefabs/Board/[BlockSpawner].prefab\n" +
                  "- Assets/Prefabs/UI/BackgroundCanvas.prefab");
    }

    // ==================== GameOverPanel ====================
    private static void CreateGameOverPanelPrefab()
    {
        string path = "Assets/Prefabs/UI/GameOverPanel.prefab";

        // 创建根节点
        var panelGo = new GameObject("GameOverPanel");
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);

        // FinalScoreText
        var txtGo = new GameObject("FinalScoreText");
        txtGo.transform.SetParent(panelGo.transform, false);
        var txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0.5f, 0.6f);
        txtRect.anchorMax = new Vector2(0.5f, 0.6f);
        txtRect.pivot = new Vector2(0.5f, 0.6f);
        txtRect.anchoredPosition = Vector2.zero;
        txtRect.sizeDelta = new Vector2(600, 200);

        var txt = txtGo.AddComponent<Text>();
        txt.text = "Final Score\n0";
        txt.fontSize = 56;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        // RestartButton
        var btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(panelGo.transform, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.35f);
        btnRect.anchorMax = new Vector2(0.5f, 0.35f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = Vector2.zero;
        btnRect.sizeDelta = new Vector2(350, 100);

        var btnImage = btnGo.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.7f, 0.4f, 1f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImage;

        // 按钮文字
        var btnTxtGo = new GameObject("BtnText");
        btnTxtGo.transform.SetParent(btnGo.transform, false);
        var btnTxtRect = btnTxtGo.AddComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero;
        btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.pivot = new Vector2(0.5f, 0.5f);
        btnTxtRect.offsetMin = Vector2.zero;
        btnTxtRect.offsetMax = Vector2.zero;

        var btnTxt = btnTxtGo.AddComponent<Text>();
        btnTxt.text = "Restart";
        btnTxt.fontSize = 40;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.color = Color.white;
        btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (btnTxt.font == null) btnTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        panelGo.SetActive(false); // 默认隐藏

        PrefabUtility.SaveAsPrefabAsset(panelGo, path);
        Object.DestroyImmediate(panelGo);
        Debug.Log($"[CreateGamePrefabs] 已创建: {path}");
    }

    // ==================== ScoreDisplay (NumberImageDisplay) ====================
    private static void CreateScoreDisplayPrefab(string name, float digitW, float digitH, string path)
    {
        var go = new GameObject(name);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.53f, 0.86f);
        rect.anchorMax = new Vector2(0.5f, 0.9f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -80f);

        var display = go.AddComponent<BlockPuzzle.UI.NumberImageDisplay>();
        display.DigitWidth = digitW;
        display.DigitHeight = digitH;
        display.TextAlignment = BlockPuzzle.UI.NumberImageDisplay.Alignment.Center;

        // 尝试加载数字精灵
        LoadDigitSprites(display);

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[CreateGamePrefabs] 已创建: {path}");
    }

    // ==================== HighScoreDisplay (NumberImageDisplay + icon) ====================
    private static void CreateHighScoreDisplayPrefab()
    {
        string path = "Assets/Prefabs/UI/HighScoreDisplay.prefab";

        var go = new GameObject("HighScoreDisplay");
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.54f, 0.835f);
        rect.anchorMax = new Vector2(0.5f, 0.98f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -30f);

        var display = go.AddComponent<BlockPuzzle.UI.NumberImageDisplay>();
        display.DigitWidth = 44f;
        display.DigitHeight = 72f;
        display.TextAlignment = BlockPuzzle.UI.NumberImageDisplay.Alignment.Center;

        LoadDigitSprites(display);

        // 最高分图标
        var iconSprite = LoadHighScoreIconSprite();
        if (iconSprite != null)
        {
            var iconGo = new GameObject("HighScoreIcon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(1f, 0.5f);
            iconRect.sizeDelta = new Vector2(72f, 64f);
            iconRect.anchoredPosition = new Vector2(-118.9f, 5.1f);

            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[CreateGamePrefabs] 已创建: {path}");
    }

    // ==================== CandidateSlot ====================
    private static void CreateCandidateSlotPrefab()
    {
        string path = "Assets/Prefabs/Board/CandidateSlot.prefab";
        // 如果已存在 CandidateSlot 就跳过（已有一个了）
        if (File.Exists(path))
        {
            Debug.Log($"[CreateGamePrefabs] 已存在，跳过: {path}");
            return;
        }

        var go = new GameObject("CandidateSlot");

        // 底板 Sprite
        var boardSprite = LoadCandidateBoardSprite();
        if (boardSprite != null)
        {
            var boardGo = new GameObject("Board");
            boardGo.transform.SetParent(go.transform, false);
            var sr = boardGo.AddComponent<SpriteRenderer>();
            sr.sprite = boardSprite;
            sr.sortingOrder = 4;
        }

        // BlockAnchor（方块挂载点）
        var anchorGo = new GameObject("BlockAnchor");
        anchorGo.transform.SetParent(go.transform, false);

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[CreateGamePrefabs] 已创建: {path}");
    }

    // ==================== BlockSpawner ====================
    private static void CreateBlockSpawnerPrefab()
    {
        string path = "Assets/Prefabs/Board/[BlockSpawner].prefab";

        var go = new GameObject("[BlockSpawner]");
        var spawner = go.AddComponent<BlockPuzzle.Block.BlockSpawner>();

        // 配置 BlockCell Prefab 引用
        var blockCellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Block/BlockCell.prefab");
        if (blockCellPrefab != null)
        {
            var field = typeof(BlockPuzzle.Block.BlockSpawner).GetField("_blockCellPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(spawner, blockCellPrefab);
        }

        // 配置候选区底板 Sprite
        var boardSprite = LoadCandidateBoardSprite();
        if (boardSprite != null)
        {
            var field = typeof(BlockPuzzle.Block.BlockSpawner).GetField("_candidateBoardSprite",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(spawner, boardSprite);
        }

        // 配置候选槽位 Prefab
        var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Board/CandidateSlot.prefab");
        if (slotPrefab != null)
        {
            var field = typeof(BlockPuzzle.Block.BlockSpawner).GetField("_candidateSlotPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(spawner, slotPrefab);
        }

        // 配置底板大小
        var sizeField = typeof(BlockPuzzle.Block.BlockSpawner).GetField("_candidateBoardSize",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        sizeField?.SetValue(spawner, 1f);

        // 配置宽松拖拽
        var dragField = typeof(BlockPuzzle.Block.BlockSpawner).GetField("_looseCandidateDrag",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        dragField?.SetValue(spawner, true);

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[CreateGamePrefabs] 已创建: {path}");
    }

    // ==================== BackgroundCanvas ====================
    private static void CreateBackgroundCanvasPrefab()
    {
        string path = "Assets/Prefabs/UI/BackgroundCanvas.prefab";

        var canvasGo = new GameObject("BackgroundCanvas");

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.planeDistance = 50f;
        canvas.sortingOrder = -10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 1f;

        // 背景图（全屏拉伸）
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/拆分资源/BJ.png");
        if (bgSprite == null)
            bgSprite = Resources.Load<Sprite>("Art/Backgrounds/bg_game");

        var bgImageGo = new GameObject("BackgroundImage");
        bgImageGo.transform.SetParent(canvasGo.transform, false);

        var bgRect = bgImageGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var bgImage = bgImageGo.AddComponent<Image>();
        if (bgSprite != null) bgImage.sprite = bgSprite;
        bgImage.type = Image.Type.Simple;
        bgImage.preserveAspect = false;
        bgImage.raycastTarget = false;

        PrefabUtility.SaveAsPrefabAsset(canvasGo, path);
        Object.DestroyImmediate(canvasGo);
        Debug.Log($"[CreateGamePrefabs] 已创建: {path}");
    }

    // ==================== CandidateBoard（候选区底板） ====================
    private static void CreateCandidateBoardPrefab()
    {
        string path = "Assets/Prefabs/Board/CandidateBoard.prefab";

        var go = new GameObject("CandidateBoard");

        // 底板 Sprite
        var boardSprite = LoadCandidateBoardSprite();
        if (boardSprite != null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = boardSprite;
            sr.sortingOrder = 4;
        }

        // 默认大小：和之前 fallback 一致（scale=1，由代码设位置）
        // 美术可在 Prefab 中添加装饰元素、调整大小等

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[CreateGamePrefabs] 已创建: {path}");
    }

    // ==================== 辅助 ====================

    private static void LoadDigitSprites(BlockPuzzle.UI.NumberImageDisplay display)
    {
        var sprites = new Sprite[10];
        bool allLoaded = true;
        for (int i = 0; i <= 9; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Digits/SH2_{i}.png");
            if (sprites[i] == null)
            {
                sprites[i] = Resources.Load<Sprite>($"Digits/SH2_{i}");
            }
            if (sprites[i] == null) allLoaded = false;
        }

        if (allLoaded)
        {
            var field = typeof(BlockPuzzle.UI.NumberImageDisplay).GetField("_numberSprites",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(display, sprites);
        }
    }

    private static Sprite LoadHighScoreIconSprite()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/拆分资源/icon_HG.png");
        return sprite;
    }

    private static Sprite LoadCandidateBoardSprite()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/拆分资源/DB_01.png");
        return sprite;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
