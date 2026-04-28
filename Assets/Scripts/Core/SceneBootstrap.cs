using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Board;
using BlockPuzzle.Block;
using BlockPuzzle.Score;
using BlockPuzzle.UI;

namespace BlockPuzzle.Core
{
    /// <summary>
    /// 场景启动器：自动创建并配置所有运行时所需的管理器和UI。
    /// 将此脚本挂载到场景中的任意空 GameObject 上即可。
    /// 所有配置项都可在 Inspector 中调整，为空/默认值时走代码创建 fallback。
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        // ==================== Prefab 配置 ====================
        [Header("游戏元素 Prefab（可选，为空时代码创建 fallback）")]
        [Tooltip("棋盘格子 Prefab（需含 SpriteRenderer）")]
        [SerializeField] private GameObject _cellPrefab;

        [Tooltip("预览格子 Prefab（需含 SpriteRenderer）")]
        [SerializeField] private GameObject _previewPrefab;

        [Tooltip("方块单格 Prefab（需含 SpriteRenderer）")]
        [SerializeField] private GameObject _blockCellPrefab;

        [Tooltip("候选区黑色底板 Sprite（DB_01.png）。无 Slot Prefab 时的 fallback。")]
        [SerializeField] private Sprite _candidateBoardSprite;

        [Tooltip("候选槽位 Prefab（可选）。美术可在 Prefab 内自由调整底板大小/位置/装饰。\n"
               + "约定：Prefab 内名为 'BlockAnchor' 的子对象将作为方块的挂载点（找不到则挂在根节点下）。\n"
               + "为空时走代码创建 + Sprite 底板 fallback。")]
        [SerializeField] private GameObject _candidateSlotPrefab;

        // ==================== UI Prefab 配置 ====================
        [Header("UI Prefab（可选，为空时代码创建 fallback）")]
        [Tooltip("积分显示 Prefab（需含 NumberImageDisplay 组件）。为空时代码创建。")]
        [SerializeField] private GameObject _scoreDisplayPrefab;

        [Tooltip("GameOver 面板 Prefab（需含子对象 FinalScoreText(Text) 和 RestartButton(Button)）")]
        [SerializeField] private GameObject _gameOverPanelPrefab;

        [Tooltip("飘字 Prefab（需含 Text + Outline 组件）。可在 Prefab 中调整字体、字号、描边样式。")]
        [SerializeField] private GameObject _floatingScorePrefab;

        // ==================== 布局配置（在 Inspector 中调整） ====================
        [Header("棋盘布局")]
        [Tooltip("棋盘中心的世界坐标")]
        [SerializeField] private Vector3 _boardCenter = new Vector3(0f, 1.1f, 0f);

        [Tooltip("每个格子的世界单位大小")]
        [SerializeField] private float _cellSize = 1.0f;

        [Tooltip("格子之间的间距")]
        [SerializeField] private float _cellSpacing = 0.08f;

        [Header("候选区布局")]
        [Tooltip("候选区中心的世界坐标")]
        [SerializeField] private Vector3 _candidateCenter = new Vector3(0f, -8.2f, 0f);

        [Tooltip("候选方块之间的水平间距")]
        [SerializeField] private float _candidateSpacing = 3.6f;

        [Tooltip("候选方块的缩放比例（相对于棋盘格子大小）")]
        [SerializeField] private float _candidateScale = 0.55f;

        [Tooltip("候选区背景底板大小（世界单位）。值越大，底板越大。默认 4.5，建议范围 3.0~6.0。")]
        [SerializeField] private float _candidateBoardSize = 4.5f;

        // ==================== 分数显示布局配置 ====================
        [Header("分数显示布局")]
        [SerializeField] private Vector2 _scoreAnchorMin = new Vector2(0.5f, 0.92f);
        [SerializeField] private Vector2 _scoreAnchorMax = new Vector2(0.5f, 0.92f);
        [SerializeField] private Vector2 _scoreAnchoredPosition = new Vector2(0f, -80f);
        [SerializeField] private float _scoreDigitWidth = 50f;
        [SerializeField] private float _scoreDigitHeight = 70f;
        [SerializeField] private NumberImageDisplay.Alignment _scoreAlignment = NumberImageDisplay.Alignment.Center;

        // ==================== 最高分显示布局配置 ====================
        [Header("最高分显示布局")]
        [Tooltip("最高分锚点最小值")]
        [SerializeField] private Vector2 _highScoreAnchorMin = new Vector2(0.5f, 0.98f);
        [Tooltip("最高分锚点最大值")]
        [SerializeField] private Vector2 _highScoreAnchorMax = new Vector2(0.5f, 0.98f);
        [Tooltip("最高分位置偏移")]
        [SerializeField] private Vector2 _highScoreAnchoredPosition = new Vector2(0f, -30f);
        [Tooltip("最高分数字宽度")]
        [SerializeField] private float _highScoreDigitWidth = 35f;
        [Tooltip("最高分数字高度")]
        [SerializeField] private float _highScoreDigitHeight = 50f;
        [Tooltip("最高分对齐方式")]
        [SerializeField] private NumberImageDisplay.Alignment _highScoreAlignment = NumberImageDisplay.Alignment.Center;

        // ==================== 最高分图标配置 ====================
        [Header("最高分图标（装饰）")]
        [Tooltip("最高分图标 Sprite（icon_HG.png）。为空时自动从 Assets/Art/拆分资源/ 加载。")]
        [SerializeField] private Sprite _highScoreIconSprite;

        [Tooltip("图标大小（宽 x 高，像素单位）")]
        [SerializeField] private Vector2 _highScoreIconSize = new Vector2(45f, 45f);

        [Tooltip("图标相对数字左侧的 X 偏移量（像素单位，负值=更靠左）")]
        [SerializeField] private float _highScoreIconOffsetX = -10f;

        [Tooltip("图标相对数字垂直居中的 Y 偏移量（像素单位，正值=上移，负值=下移）")]
        [SerializeField] private float _highScoreIconOffsetY = 0f;

        // ==================== 分数区域底板配置 ====================
        [Header("分数区域底板（装饰）")]
        [Tooltip("底板 Sprite（DB_ZGF.png）。为空时自动从 Assets/Art/拆分资源/ 加载。")]
        [SerializeField] private Sprite _scoreAreaBgSprite;

        [Tooltip("底板大小（宽 x 高，像素单位）")]
        [SerializeField] private Vector2 _scoreAreaBgSize = new Vector2(400f, 180f);

        [Tooltip("底板位置（anchoredPosition，像素单位）")]
        [SerializeField] private Vector2 _scoreAreaBgPosition = new Vector2(0f, -50f);

        // ==================== 显示格式配置 ====================
        [Header("分数显示格式")]
        [Tooltip("游戏中分数格式。{0}=分数数字。例：\"Score: {0}\"、\"分数\\n{0}\"、\"{0}\"")]
        [SerializeField] private string _scoreFormat = "Score: {0}";

        [Tooltip("结算分数格式。{0}=分数数字。例：\"Final Score\\n{0}\"、\"最终得分\\n{0}\"")]
        [SerializeField] private string _finalScoreFormat = "Final Score\n{0}";

        // ==================== HUD 按钮配置 ====================
        [Header("HUD 按钮 Prefab（可选）")]
        [Tooltip("重新开始按钮 Prefab（需含 Image + Button 组件）。为空时不显示。")]
        [SerializeField] private GameObject _restartButtonPrefab;

        /// <summary>分数区域底板缓存引用（供 OnValidate 热更新用）</summary>
        private GameObject _scoreAreaBgGo;

        /// <summary>最高分图标缓存引用（供 OnValidate 热更新用）</summary>
        private GameObject _highScoreIconGo;

        private void Awake()
        {
            // 用 Inspector 配置覆盖全局常量（必须在其他初始化之前）
            ApplyLayoutConfig();

            SetupCamera();
            CreateBackgroundUI();
            CreateManagers();
            CreateUI();
        }

        /// <summary>
        /// 将 Inspector 中的布局配置写入 Constants，供所有系统使用
        /// </summary>
        private void ApplyLayoutConfig()
        {
            Utils.Constants.BoardCenter = _boardCenter;
            Utils.Constants.CellSize = _cellSize;
            Utils.Constants.CellSpacing = _cellSpacing;
            Utils.Constants.CandidateCenter = _candidateCenter;
            Utils.Constants.CandidateSpacing = _candidateSpacing;
            Utils.Constants.CandidateScale = _candidateScale;
        }

        // ==================== 配置相机 ====================

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.orthographic = true;
            cam.orthographicSize = 11f; // 竖屏：上下各11个世界单位
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.15f, 0.12f, 0.10f, 1f); // 棕色 fallback（匹配背景图色调）
        }

        // ==================== 创建背景（UI Canvas 方式） ====================

        private void CreateBackgroundUI()
        {
            var bgSprite = Utils.SpriteUtils.BackgroundSprite;
            if (bgSprite == null) return;

            // 创建独立的背景 Canvas（ScreenSpaceCamera 模式，渲染在世界空间之后）
            var bgCanvasGo = new GameObject("BackgroundCanvas");
            var bgCanvas = bgCanvasGo.AddComponent<Canvas>();
            bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            bgCanvas.worldCamera = Camera.main;
            bgCanvas.planeDistance = 50f; // 足够远，在棋盘/方块之后
            bgCanvas.sortingOrder = -10; // 最底层

            var bgScaler = bgCanvasGo.AddComponent<CanvasScaler>();
            bgScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            bgScaler.referenceResolution = new Vector2(1080, 1920);
            bgScaler.matchWidthOrHeight = 1f; // 以高度为基准（竖屏）

            // 创建全屏 Image
            var bgImageGo = new GameObject("BackgroundImage");
            bgImageGo.transform.SetParent(bgCanvasGo.transform, false);

            var bgRect = bgImageGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgImageGo.AddComponent<Image>();
            bgImage.sprite = bgSprite;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false; // 拉伸填满
            bgImage.raycastTarget = false;  // 不阻挡点击
        }

        // ==================== 创建管理器 ====================

        private void CreateManagers()
        {
            // BoardManager
            if (FindFirstObjectByType<BoardManager>() == null)
            {
                var boardGo = new GameObject("[BoardManager]");
                boardGo.AddComponent<BoardManager>();
            }
            // 注入 Prefab 引用
            if (BoardManager.Instance != null)
            {
                BoardManager.Instance.SetCellPrefab(_cellPrefab);
                BoardManager.Instance.SetPreviewPrefab(_previewPrefab);
            }

            // BlockSpawner
            if (FindFirstObjectByType<BlockSpawner>() == null)
            {
                var spawnerGo = new GameObject("[BlockSpawner]");
                spawnerGo.AddComponent<BlockSpawner>();
            }
            // 注入 Prefab 引用
            if (BlockSpawner.Instance != null)
            {
                BlockSpawner.Instance.SetBlockCellPrefab(_blockCellPrefab);
                BlockSpawner.Instance.SetCandidateBoardSprite(_candidateBoardSprite);
                BlockSpawner.Instance.SetCandidateBoardSize(_candidateBoardSize);
                BlockSpawner.Instance.SetCandidateSlotPrefab(_candidateSlotPrefab);
            }

            // ScoreManager
            if (FindFirstObjectByType<ScoreManager>() == null)
            {
                var scoreGo = new GameObject("[ScoreManager]");
                scoreGo.AddComponent<ScoreManager>();
            }

            // GameManager（最后创建，因为 Start 中会初始化各系统）
            if (FindFirstObjectByType<GameManager>() == null)
            {
                var gmGo = new GameObject("[GameManager]");
                gmGo.AddComponent<GameManager>();
            }
        }

        // ==================== 创建 UI ====================

        private void CreateUI()
        {
            // 创建 Canvas
            var canvasGo = new GameObject("GameCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // --- 分数区域装饰底板 ---
            _scoreAreaBgGo = CreateScoreAreaBackground(canvasGo.transform);

            // --- 分数显示 ---
            GameObject scoreDisplayGo;
            if (_scoreDisplayPrefab != null && _scoreDisplayPrefab.GetComponent<NumberImageDisplay>() != null)
            {
                scoreDisplayGo = Instantiate(_scoreDisplayPrefab, canvasGo.transform, false);
                scoreDisplayGo.name = "ScoreDisplay";
                scoreDisplayGo.SetActive(true); // 确保激活（Prefab 可能是 inactive 的）
                // 应用布局参数到实例
                var display = scoreDisplayGo.GetComponent<NumberImageDisplay>();
                display.DigitWidth = _scoreDigitWidth;
                display.DigitHeight = _scoreDigitHeight;
                display.TextAlignment = _scoreAlignment;
                var rect = scoreDisplayGo.GetComponent<RectTransform>();
                rect.anchorMin = _scoreAnchorMin;
                rect.anchorMax = _scoreAnchorMax;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = _scoreAnchoredPosition;
            }
            else
            {
                scoreDisplayGo = CreateNumberImageDisplay(canvasGo.transform,
                    _scoreAnchorMin, _scoreAnchorMax, _scoreAnchoredPosition,
                    _scoreDigitWidth, _scoreDigitHeight, _scoreAlignment);
            }

            // 自动加载数字精灵（如果 Prefab 没有配置）
            EnsureDigitSprites(scoreDisplayGo);

            // --- 最高分显示 ---
            GameObject highScoreDisplayGo = CreateNumberImageDisplay(canvasGo.transform,
                _highScoreAnchorMin, _highScoreAnchorMax, _highScoreAnchoredPosition,
                _highScoreDigitWidth, _highScoreDigitHeight, _highScoreAlignment);
            highScoreDisplayGo.name = "HighScoreDisplay";
            EnsureDigitSprites(highScoreDisplayGo);

            // --- 最高分图标（在数值前面） ---
            _highScoreIconGo = CreateHighScoreIcon(highScoreDisplayGo.transform);

            // --- 游戏结束面板 ---
            GameObject gameOverPanel;
            if (_gameOverPanelPrefab != null)
            {
                gameOverPanel = Instantiate(_gameOverPanelPrefab, canvasGo.transform, false);
                gameOverPanel.name = "GameOverPanel";
                gameOverPanel.SetActive(false);
            }
            else
            {
                gameOverPanel = CreateGameOverPanel(canvasGo.transform);
            }

            // --- 挂载 GameUI 脚本 ---
            var gameUI = canvasGo.AddComponent<GameUI>();

            // 通过反射设置私有字段（因为是 SerializeField）
            SetPrivateField(gameUI, "_scoreDisplay", scoreDisplayGo.GetComponent<NumberImageDisplay>());
            SetPrivateField(gameUI, "_highScoreDisplay", highScoreDisplayGo.GetComponent<NumberImageDisplay>());
            SetPrivateField(gameUI, "_scoreFormat", _scoreFormat);
            SetPrivateField(gameUI, "_gameOverPanel", gameOverPanel);
            SetPrivateField(gameUI, "_finalScoreText", gameOverPanel.transform.Find("FinalScoreText")?.GetComponent<Text>());
            SetPrivateField(gameUI, "_restartButton", gameOverPanel.transform.Find("RestartButton")?.GetComponent<Button>());
            SetPrivateField(gameUI, "_finalScoreFormat", _finalScoreFormat);

            // --- HUD 重新开始按钮（左上角） ---
            if (_restartButtonPrefab != null)
            {
                var hudRestartGo = Instantiate(_restartButtonPrefab, canvasGo.transform, false);
                hudRestartGo.name = "HudRestartButton";
                var hudBtn = hudRestartGo.GetComponent<Button>();
                if (hudBtn == null) hudBtn = hudRestartGo.AddComponent<Button>();
                hudBtn.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.RestartGame();
                });
            }

            // --- 得分飘字管理器 ---
            var floatingMgr = canvasGo.AddComponent<FloatingScoreManager>();
            floatingMgr.Init(canvas);
            floatingMgr.SetFloatingScorePrefab(_floatingScorePrefab);

            // 监听消除计分事件，驱动飘字（设计文档 123-131 流程）
            // ① 放置分飘字 → ② 消除分飘字 → ③ Combo 飘字 → ④ 播完 → 总分跳动
            if (Score.ScoreManager.Instance != null)
            {
                Score.ScoreManager.Instance.OnLineClearScoreDetail += (lineCount, baseScore, comboBonus, comboCount) =>
                {
                    // ① 显示放置分飘字（如 "+4"）— 从 _lastPlacementScore 反推 cellCount（baseScore 为 long，需显式转 int）
                    floatingMgr.EnqueuePlacementScore(baseScore > 0 ? (int)(baseScore / (lineCount * 2 - 1)) : lineCount);
                    // ② 逐行显示消除分飘字（如 "×2 +16"、"+256"）
                    floatingMgr.EnqueueClearScore(baseScore, lineCount);
                    // ③ 如果有 Combo → 显示 Combo 加成飘字（如 "Combo ×1 → ×1.2"）
                    if (comboCount > 0)
                        floatingMgr.EnqueueComboBonus(comboCount, Utils.Constants.GetComboMultiplier(comboCount));
                    // 开始播放所有飘字
                    floatingMgr.PlayAll();
                };

                // ④ 所有飘字展示完毕后 → 总分数字有跳动效果
                floatingMgr.OnAllFinished += () =>
                {
                    var gameUI = FindFirstObjectByType<GameUI>();
                    if (gameUI != null)
                    {
                        var scoreField = typeof(GameUI).GetField("_scoreDisplay",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var display = scoreField?.GetValue(gameUI) as NumberImageDisplay;
                        display?.PlayBounceEffect();
                    }
                };
            }
        }

        /// <summary>用代码创建 NumberImageDisplay（无 Prefab 时的 fallback）</summary>
        private GameObject CreateNumberImageDisplay(Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
            float digitW, float digitH, NumberImageDisplay.Alignment alignment)
        {
            var go = new GameObject("ScoreDisplay", typeof(RectTransform), typeof(NumberImageDisplay));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;

            var display = go.GetComponent<NumberImageDisplay>();
            display.DigitWidth = digitW;
            display.DigitHeight = digitH;
            display.TextAlignment = alignment;

            return go;
        }

        /// <summary>创建分数区域装饰底板（DB_ZGF.png）</summary>
        private GameObject CreateScoreAreaBackground(Transform parent)
        {
            var sprite = _scoreAreaBgSprite ?? LoadScoreAreaBgSprite();
            if (sprite == null) return null;

            var go = new GameObject("ScoreAreaBackground", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = _scoreAreaBgSize;
            rect.anchoredPosition = _scoreAreaBgPosition;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced; // 支持九宫格拉伸（美术可自行设置）
            img.raycastTarget = false;    // 装饰用，不阻挡点击

            return go;
        }

        /// <summary>加载分数区域底板 Sprite（Assets/Art/拆分资源/DB_ZGF.png）</summary>
        private static Sprite LoadScoreAreaBgSprite()
        {
            // 优先 Resources
            var sprite = Resources.Load<Sprite>("Art/拆分资源/DB_ZGF");
#if UNITY_EDITOR
            // Editor 下 fallback 到 AssetDatabase
            if (sprite == null)
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/拆分资源/DB_ZGF.png");
#endif
            return sprite;
        }

        /// <summary>创建最高分装饰图标（icon_HG.png），作为 HighScoreDisplay 的子对象</summary>
        private GameObject CreateHighScoreIcon(Transform parent)
        {
            var sprite = _highScoreIconSprite ?? LoadHighScoreIconSprite();
            if (sprite == null) return null;

            var go = new GameObject("HighScoreIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f); // 右锚点，便于用 offset 定位到数字左侧
            rect.sizeDelta = _highScoreIconSize;
            // 位置：在数字左侧，offsetX/offsetY 控制位置
            rect.anchoredPosition = new Vector2(_highScoreIconOffsetX, _highScoreIconOffsetY);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false; // 装饰用

            return go;
        }

        /// <summary>加载最高分图标 Sprite（Assets/Art/拆分资源/icon_HG.png）</summary>
        private static Sprite LoadHighScoreIconSprite()
        {
            var sprite = Resources.Load<Sprite>("Art/拆分资源/icon_HG");
#if UNITY_EDITOR
            if (sprite == null)
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/拆分资源/icon_HG.png");
#endif
            return sprite;
        }

        /// <summary>
        /// 自动加载 SH2 数字精灵到 NumberImageDisplay。
        /// 如果组件已有有效精灵则跳过（Inspector 已配置）。
        /// </summary>
        private void EnsureDigitSprites(GameObject scoreGo)
        {
            if (scoreGo == null) return;
            var display = scoreGo.GetComponent<NumberImageDisplay>();
            if (display == null || display.HasValidSprites) return;

            // 尝试从 Resources 加载 SH2_0~9 精灵
            var sprites = new Sprite[10];
            bool allLoaded = true;
            for (int i = 0; i <= 9; i++)
            {
                sprites[i] = Resources.Load<Sprite>($"Digits/SH2_{i}");
                if (sprites[i] == null) allLoaded = false;
            }

            if (allLoaded)
                SetPrivateField(display, "_numberSprites", sprites);
        }

        private GameObject CreateGameOverPanel(Transform parent)
        {
            // 半透明背景面板
            var panelGo = new GameObject("GameOverPanel");
            panelGo.transform.SetParent(parent, false);

            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            // 最终分数文本
            CreateUIText(panelGo.transform, "FinalScoreText", "Final Score\n0",
                new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
                Vector2.zero, new Vector2(600, 200), 56, TextAnchor.MiddleCenter, Color.white);

            // 重新开始按钮
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
            CreateUIText(btnGo.transform, "BtnText", "Restart",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 40, TextAnchor.MiddleCenter, Color.white, true);

            panelGo.SetActive(false);
            return panelGo;
        }

        // ==================== UI 工具方法 ====================

        private GameObject CreateUIText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta, int fontSize,
            TextAnchor alignment, Color color, bool stretch = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            if (!stretch) rect.sizeDelta = sizeDelta;
            else
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            // Unity 6 使用 LegacyRuntime.ttf，旧版使用 Arial.ttf
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null)
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            return go;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
        }

        // ==================== 运行时参数热更新 ====================

#if UNITY_EDITOR
        /// <summary>
        /// Inspector 中任何值被修改时自动调用。
        /// 运行时：立即将新值写入 Constants 并通知各 Manager 重新布局。
        /// 编辑器非运行时：仅更新 Constants（不触发布局，因为场景还没初始化）。
        /// </summary>
        private void OnValidate()
        {
            // 将 Inspector 值同步到 Constants
            ApplyLayoutConfig();

            // 仅在运行时执行实时布局刷新
            if (!Application.isPlaying) return;

            // 延迟一帧执行，避免 OnValidate 中直接操作 GameObject 的限制
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return; // 防止脚本已被销毁

                // 刷新棋盘布局
                if (BoardManager.Instance != null)
                    BoardManager.Instance.RelayoutBoard();

                // 刷新候选区布局（含底板大小）
                if (BlockSpawner.Instance != null)
                {
                    BlockSpawner.Instance.SetCandidateBoardSize(_candidateBoardSize);
                    BlockSpawner.Instance.RelayoutCandidates();
                }

                // 刷新分数格式显示
                var gameUI = FindFirstObjectByType<GameUI>();
                if (gameUI != null)
                {
                    SetPrivateField(gameUI, "_scoreFormat", _scoreFormat);
                    SetPrivateField(gameUI, "_finalScoreFormat", _finalScoreFormat);
                    // 立即用新格式刷新当前分数显示
                    if (ScoreManager.Instance != null)
                        gameUI.RefreshDisplay(ScoreManager.Instance.CurrentScore);
                }

                // 刷新分数显示布局参数
                RefreshScoreDisplayLayout();
                RefreshHighScoreDisplayLayout();
                RefreshScoreAreaBgLayout();
                RefreshHighScoreIconLayout();
            };
        }

        /// <summary>运行时热更新分数显示的布局参数</summary>
        private void RefreshScoreDisplayLayout()
        {
            var gameUI = FindFirstObjectByType<GameUI>();
            if (gameUI == null) return;
            // 通过反射获取 _scoreDisplay 字段
            var field = typeof(GameUI).GetField("_scoreDisplay",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var display = field?.GetValue(gameUI) as NumberImageDisplay;
            if (display == null) return;

            display.DigitWidth = _scoreDigitWidth;
            display.DigitHeight = _scoreDigitHeight;
            display.TextAlignment = _scoreAlignment;
            var rect = display.GetComponent<RectTransform>();
            rect.anchorMin = _scoreAnchorMin;
            rect.anchorMax = _scoreAnchorMax;
            rect.anchoredPosition = _scoreAnchoredPosition;
        }

        /// <summary>运行时热更新最高分显示的布局参数</summary>
        private void RefreshHighScoreDisplayLayout()
        {
            var gameUI = FindFirstObjectByType<GameUI>();
            if (gameUI == null) return;
            var field = typeof(GameUI).GetField("_highScoreDisplay",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var display = field?.GetValue(gameUI) as NumberImageDisplay;
            if (display == null) return;

            display.DigitWidth = _highScoreDigitWidth;
            display.DigitHeight = _highScoreDigitHeight;
            display.TextAlignment = _highScoreAlignment;
            var rect = display.GetComponent<RectTransform>();
            rect.anchorMin = _highScoreAnchorMin;
            rect.anchorMax = _highScoreAnchorMax;
            rect.anchoredPosition = _highScoreAnchoredPosition;
        }

        /// <summary>运行时热更新分数区域底板的布局参数</summary>
        private void RefreshScoreAreaBgLayout()
        {
            if (_scoreAreaBgGo == null) return;
            var rect = _scoreAreaBgGo.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.sizeDelta = _scoreAreaBgSize;
            rect.anchoredPosition = _scoreAreaBgPosition;

            // 如果 Inspector 手动换了 Sprite 也同步更新
            if (_scoreAreaBgSprite != null)
            {
                var img = _scoreAreaBgGo.GetComponent<Image>();
                if (img != null) img.sprite = _scoreAreaBgSprite;
            }
        }

        /// <summary>运行时热更新最高分图标的布局参数</summary>
        private void RefreshHighScoreIconLayout()
        {
            if (_highScoreIconGo == null) return;
            var rect = _highScoreIconGo.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.sizeDelta = _highScoreIconSize;
            rect.anchoredPosition = new Vector2(_highScoreIconOffsetX, _highScoreIconOffsetY);

            if (_highScoreIconSprite != null)
            {
                var img = _highScoreIconGo.GetComponent<Image>();
                if (img != null) img.sprite = _highScoreIconSprite;
            }
        }
#endif
    }
}
